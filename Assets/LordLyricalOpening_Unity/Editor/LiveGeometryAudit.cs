using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;

public static class LiveGeometryAudit
{
    public const string Out=MotionPolishValidation.Out+"/Production";
    [MenuItem("Lord Lyrical/Production/Audit Live Geometry")]
    public static void Run()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("LIVE GEOMETRY AND SKINNING AUDIT\nEditor estimates at the participant mark; no forced LOD. Native counters remain authoritative.\n");
        try {
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Audit session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Audit cast").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var camera=new GameObject("Participant view").AddComponent<Camera>();camera.fieldOfView=90;camera.nearClipPlane=.08f;camera.farClipPlane=100;
            camera.transform.position=new Vector3(0,1.65f,2);camera.transform.rotation=Quaternion.identity;
            report.AppendLine("Mesh deformation: "+PlayerSettings.meshDeformation);
            foreach(float time in new[]{31f,41f,47.4f,75f,95f}) {
                world.Render(ShowFrame.At(time),true);
                var planes=GeometryUtility.CalculateFrustumPlanes(camera);
                var selection=new Dictionary<Renderer,bool>();
                foreach(var group in world.GetComponentsInChildren<LODGroup>(false)) {
                    float size=group.size*Mathf.Max(group.transform.lossyScale.x,group.transform.lossyScale.y,group.transform.lossyScale.z);
                    float distance=Vector3.Distance(camera.transform.position,group.transform.TransformPoint(group.localReferencePoint));
                    float relative=size/(2*Mathf.Max(.01f,distance)*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f))*QualitySettings.lodBias;
                    var lods=group.GetLODs();int chosen=Array.FindIndex(lods,l=>relative>=l.screenRelativeTransitionHeight);
                    for(int i=0;i<lods.Length;i++)foreach(var r in lods[i].renderers)if(r)selection[r]=selection.GetValueOrDefault(r)||i==chosen;
                    report.AppendLine($"  {time:F1}s LOD {group.name}: size={size:F2} distance={distance:F2} relative={relative:F3} level={chosen}");
                }
                var rows=new List<(string name,long triangles)>();
                int skins=0,offscreen=0;long vertices=0;
                foreach(var renderer in world.GetComponentsInChildren<Renderer>(false)) {
                    if(renderer is SkinnedMeshRenderer sk0){skins++;if(sk0.updateWhenOffscreen){offscreen++;vertices+=sk0.sharedMesh?sk0.sharedMesh.vertexCount:0;}}
                    if(!renderer.enabled||renderer.forceRenderingOff||selection.TryGetValue(renderer,out bool selected)&&!selected||!GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;
                    var filter=renderer.GetComponent<MeshFilter>();
                    var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:filter?filter.sharedMesh:null;
                    if(!mesh)continue;long tris=0;for(int sub=0;sub<mesh.subMeshCount;sub++)tris+=(long)mesh.GetIndexCount(sub)/3;
                    rows.Add((renderer.name,tris));
                }
                report.AppendLine($"TIME {time:F1}: active skin renderers={skins}, updateOffscreen={offscreen}, offscreen vertices={vertices}, estimated visible triangles={rows.Sum(x=>x.triangles)}");
                foreach(var row in rows.OrderByDescending(x=>x.triangles).Take(28))report.AppendLine($"    {row.name}: {row.triangles}");
            }
            var orc=world.GetComponentInChildren<SonicOrcPerformance>(true);
            foreach(var skin in orc.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                report.AppendLine($"ORC {skin.name}: vertices={skin.sharedMesh.vertexCount}, bounds={skin.localBounds}, offscreen={skin.updateWhenOffscreen}, root={(skin.rootBone?skin.rootBone.name:"none")}, motionVectors={skin.skinnedMotionVectors}");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        Directory.CreateDirectory(Out);File.WriteAllText(Out+"/Live_Geometry_Audit.txt",report.ToString());Debug.Log("LIVE_GEOMETRY_AUDIT_COMPLETE");
    }
}
