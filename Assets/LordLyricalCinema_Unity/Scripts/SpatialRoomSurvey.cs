using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.PXR;
using Unity.XR.OpenXR.Features.PICOSupport;

namespace LordLyrical
{
    // The native mesh remains in tracking space. Alignment moves its parent with the XR rig.
    // These are visual overlays: real furniture never becomes an invisible combat collider.
    public sealed class SpatialRoomSurvey : MonoBehaviour
    {
        public const int MaxPatches=80,MaxVertices=120000,MaxPending=96;
        sealed class Patch {public Mesh mesh;public Transform root;public int vertices;public Vector3[] points;public MeshRenderer overlay,occluder;}
        readonly Dictionary<Guid,Patch> patches=new();
        readonly Dictionary<Guid,PxrSpatialMeshInfo> pending=new();
        readonly List<XRMeshSubsystem> systems=new();
        readonly object sync=new();
        CancellationTokenSource cancellation;
        XRMeshSubsystem subsystem;
        Material material,occlusionMaterial;bool occlusion;
        Transform trackingOrigin;
        bool ready,dead,sceneOwned,meshOwned,subscribed,visible,allowCapture,paused;
        public string Status {get;private set;}="Waiting for spatial session";
        public bool MeshSupported {get;private set;}
        public bool SceneSupported {get;private set;}
        public bool PermissionGranted {get;private set;}
        public int SurfaceCount=>patches.Count;
        public int VertexCount {get;private set;}
        public int RejectedPatches {get;private set;}
        public bool CaptureActive {get;private set;}
        public void Initialize(Transform origin,Material shared)
        {
            trackingOrigin=origin;material=shared;occlusionMaterial=new Material(Resources.Load<Shader>("Cinema26/RoomOcclusion"));
            transform.SetParent(origin,false);
            if(Application.isPlaying)StartCoroutine(Begin());
            else Status="Editor preview — no native room";
        }
        IEnumerator Begin()
        {
            // Features are valid after the XR session exists. This also permits startup resume.
            for(int i=0;i<100&&!dead;i++){
                MeshSupported=PICOSpatialMesh.isEnable;SceneSupported=PICOSceneCapture.isEnable;
                if(MeshSupported||SceneSupported)break;
                yield return new WaitForSecondsRealtime(.1f);
            }
            if(dead)yield break;
            Debug.Log($"CINEMA26_SPATIAL mesh={MeshSupported} scene={SceneSupported}");
            if(!MeshSupported&&!SceneSupported){Status="Room mesh unavailable in this XR session";yield break;}
#if UNITY_ANDROID && !UNITY_EDITOR
            Status="Allow spatial data in the headset";
            bool answered=false;
            PXR_PermissionRequest.RequestUserPermissionMR(
                _=>{answered=true;Status="Spatial data permission declined";},
                _=>{answered=true;PermissionGranted=true;},
                _=>{answered=true;Status="Spatial data permission disabled";});
            while(!answered&&!dead)yield return null;
            if(dead||!PermissionGranted)yield break;
#else
            Status="Native room query requires a headset";yield break;
#endif
            cancellation=new CancellationTokenSource();ready=true;
            if(MeshSupported){
                SubsystemManager.GetSubsystems(systems);
                foreach(var s in systems)if(s.subsystemDescriptor.id=="PICO Mesh"){subsystem=s;break;}
                if(subsystem!=null){
                    OpenXRExtensions.SpatialMeshDataUpdated+=OnMesh;subscribed=true;
                    meshOwned=!subsystem.running;if(meshOwned)subsystem.Start();
                    Status="Mapping the real room";
                    // A captured scene can still provide walls when the live mesh is empty.
                    yield return new WaitForSecondsRealtime(3);
                }
            }
            if(!dead&&SurfaceCount==0&&SceneSupported)_=ReadScene();
        }
        async Task ReadScene()
        {
            try {
                Status="Reading saved room";
                var result=await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SceneCapture,cancellation.Token);
                if(dead)return;
                if(result!=PxrResult.SUCCESS){Status="Scene provider: "+result;return;}
                sceneOwned=true;
                var query=await PXR_MixedReality.QuerySceneAnchorAsync(new[]{PxrSemanticLabel.Floor,PxrSemanticLabel.Wall,PxrSemanticLabel.Ceiling,PxrSemanticLabel.Table,PxrSemanticLabel.Sofa},cancellation.Token);
                if(dead)return;
                // Native capture is only offered before the show, never in combat or on return.
                if(query.result==PxrResult.SUCCESS&&query.anchorHandleList.Count==0&&allowCapture){
                    Status="Map your room in the PICO room setup";CaptureActive=true;
                    var capture=await PXR_MixedReality.StartSceneCaptureAsync(cancellation.Token);
                    CaptureActive=false;if(dead)return;
                    if(capture!=PxrResult.SUCCESS){Status="Room capture: "+capture;return;}
                    query=await PXR_MixedReality.QuerySceneAnchorAsync(new[]{PxrSemanticLabel.Floor,PxrSemanticLabel.Wall,PxrSemanticLabel.Ceiling,PxrSemanticLabel.Table,PxrSemanticLabel.Sofa},cancellation.Token);
                }
                if(dead)return;
                if(query.result!=PxrResult.SUCCESS){Status="Scene query: "+query.result;return;}
                foreach(ulong handle in query.anchorHandleList){
                    if(patches.Count>=MaxPatches)break;
                    if(PXR_MixedReality.LocateAnchor(handle,out var pos,out var rot)!=PxrResult.SUCCESS)continue;
                    // Box2D is the SDK's measured extent in the anchor's local XY plane.
                    if(PXR_MixedReality.GetSceneBox2DData(handle,out var offset,out var extent)!=PxrResult.SUCCESS)continue;
                    if(extent.x<=0||extent.y<=0||extent.x>40||extent.y>40)continue;
                    var vertices=new[]{new Vector3(offset.x-extent.x/2,offset.y-extent.y/2,0),new Vector3(offset.x+extent.x/2,offset.y-extent.y/2,0),new Vector3(offset.x-extent.x/2,offset.y+extent.y/2,0),new Vector3(offset.x+extent.x/2,offset.y+extent.y/2,0)};
                    var bytes=new byte[16];BitConverter.GetBytes(handle).CopyTo(bytes,0);bytes[15]=26;
                    Apply(new PxrSpatialMeshInfo{uuid=new Guid(bytes),position=pos,rotation=rot,vertices=vertices,indices=new ushort[]{0,2,1,1,2,3},state=MeshChangeState.Added});
                }
                Status=SurfaceCount>0?"Room surfaces ready":"No room surfaces saved";
                Debug.Log($"CINEMA26_ROOM status={Status} surfaces={SurfaceCount} vertices={VertexCount}");
            }catch(OperationCanceledException){}catch(Exception e){if(!dead){Status="Room query could not finish";Debug.LogWarning("CINEMA26_ROOM "+e.Message);}}
        }
        void OnMesh(List<PxrSpatialMeshInfo> data)
        {
            lock(sync){if(dead)return;foreach(var info in data){if(pending.Count>=MaxPending&&!pending.ContainsKey(info.uuid)){RejectedPatches++;continue;}pending[info.uuid]=info;}}
        }
        void Update()
        {
            if(dead)return;
            for(int n=0;n<2;n++){
                PxrSpatialMeshInfo info=default;bool found=false;
                lock(sync){foreach(var pair in pending){info=pair.Value;found=true;break;}if(found)pending.Remove(info.uuid);}
                if(!found)break;Apply(info);
            }
        }
        public static bool Valid(Vector3[] vertices,ushort[] indices)
        {
            if(vertices==null||indices==null||vertices.Length<3||vertices.Length>MaxVertices||indices.Length<3||indices.Length%3!=0||indices.Length>MaxVertices*6)return false;
            foreach(var v in vertices)if(!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z))return false;
            foreach(var index in indices)if(index>=vertices.Length)return false;return true;
        }
        void Apply(PxrSpatialMeshInfo info)
        {
            if(info.state==MeshChangeState.Removed){
                if(patches.TryGetValue(info.uuid,out var old)){VertexCount-=old.vertices;Release(old.mesh);Release(old.root.gameObject);patches.Remove(info.uuid);}return;
            }
            if(!Valid(info.vertices,info.indices)||!ManualResonanceBeams.Finite(info.position)){RejectedPatches++;return;}
            patches.TryGetValue(info.uuid,out var patch);
            if(patch!=null&&info.state==MeshChangeState.Unchanged)return;
            if(VertexCount-(patch?.vertices??0)+info.vertices.Length>MaxVertices||(patch==null&&patches.Count>=MaxPatches)){RejectedPatches++;return;}
            if(patch==null){
                var go=new GameObject("Measured room surface");go.transform.SetParent(transform,false);
                patch=new Patch{root=go.transform,mesh=new Mesh{name="PICO measured room patch"}};
                go.AddComponent<MeshFilter>().sharedMesh=patch.mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
                patch.overlay=r;
                var depth=new GameObject("Measured depth only");depth.transform.SetParent(go.transform,false);depth.AddComponent<MeshFilter>().sharedMesh=patch.mesh;
                patch.occluder=depth.AddComponent<MeshRenderer>();patch.occluder.sharedMaterial=occlusionMaterial;patch.occluder.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;patch.occluder.receiveShadows=false;
                patches.Add(info.uuid,patch);
            }
            VertexCount-=patch.vertices;patch.mesh.Clear();patch.mesh.vertices=info.vertices;
            int[] triangles=new int[info.indices.Length];for(int i=0;i<triangles.Length;i++)triangles[i]=info.indices[i];
            patch.mesh.triangles=triangles;patch.mesh.RecalculateNormals();patch.mesh.RecalculateBounds();
            patch.root.localPosition=info.position;patch.root.localRotation=info.rotation;patch.root.gameObject.SetActive(visible||occlusion);patch.overlay.enabled=visible;patch.occluder.enabled=occlusion;
            patch.points=info.vertices;patch.vertices=info.vertices.Length;VertexCount+=patch.vertices;Status="Room surfaces ready";
        }
        public bool TrySurfacePoint(int seed,out Vector3 position)
        {
            position=default;if(patches.Count==0)return false;
            int chosen=Math.Abs(seed)%patches.Count,index=0;
            foreach(var p in patches.Values){if(index++!=chosen)continue;position=p.root.TransformPoint(p.points[Math.Abs(seed*31)%p.points.Length]);return true;}return false;
        }
        public void SetOcclusion(bool enabled,float radius)
        {
            occlusion=enabled;if(occlusionMaterial)occlusionMaterial.SetFloat("_PortalRadius",radius);
            foreach(var patch in patches.Values){patch.occluder.enabled=enabled;patch.root.gameObject.SetActive(visible||enabled);}
        }
        public void Present(bool show,bool canCapture)
        {
            allowCapture=canCapture;
            if(visible!=show){visible=show;foreach(var p in patches.Values){p.root.gameObject.SetActive(show||occlusion);p.overlay.enabled=show;}}
            // Stop native scanning during the VR concert, while retaining the measured geometry.
            bool need=canCapture&&!paused;
            if(ready&&subsystem!=null&&meshOwned){
                if(need&&!subsystem.running)subsystem.Start();
                else if(!need&&subsystem.running)subsystem.Stop();
            }
        }
        void OnApplicationPause(bool value){paused=value;if(value&&meshOwned&&subsystem!=null&&subsystem.running)subsystem.Stop();}
        static void Release(UnityEngine.Object o){if(!o)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
        void OnDestroy()
        {
            lock(sync){dead=true;pending.Clear();}
            if(subscribed)OpenXRExtensions.SpatialMeshDataUpdated-=OnMesh;
            cancellation?.Cancel();cancellation?.Dispose();
            if(meshOwned&&subsystem!=null&&subsystem.running)subsystem.Stop();
            if(sceneOwned)PXR_MixedReality.StopSenseDataProvider(PxrSenseDataProviderType.SceneCapture);
            foreach(var p in patches.Values)Release(p.mesh);patches.Clear();Release(occlusionMaterial);
        }
    }
}