using System;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.XR.OpenXR.Features.PICOSupport;
#endif
namespace LordLyrical
{
    public static class OpeningTiming
    {
        public const float Prelude=17,Arrival=-2,Landing=-.8f,PortalStart=0,EngulfStart=4,VrStart=8,VrComplete=9;
        public static float Smooth(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        public static bool UsesPassthrough(double songTime)=>double.IsNegativeInfinity(songTime)||songTime<VrComplete||songTime>=CinemaTiming.ReturnStart;
        public static float RoomOpacity(float songTime)=>songTime>=CinemaTiming.ReturnStart?1:1-Smooth(VrStart,VrComplete,songTime);
        public static float PortalRadius(float t)=>Mathf.Lerp(.03f,2.45f,Smooth(0,3.7f,t))+Smooth(4,8,t)*18;
        public static Vector3 PortalCenter=>new Vector3(0,1.7f,6.35f);
        public static Vector3 JeruPosition(float songTime)=>new Vector3(0,(1-Smooth(Arrival,Landing,songTime))*12,5);
    }
    public sealed class PassthroughOpening
    {
        readonly Camera camera;
        readonly UniversalAdditionalCameraData cameraData;
        readonly ResonantWorld world;
        readonly Transform root,portal;
        readonly Material portalMaterial;
        readonly Mesh portalMesh;
        readonly LineRenderer[] bolts=new LineRenderer[7],rims=new LineRenderer[5];
        readonly Vector3[] points=new Vector3[80];
        readonly CinematicParticleDirector particles;
        readonly Color vrColor;readonly ArrivalSoundscape arrivalAudio;
        public CinemaPresentation Cinema {get;}
        bool nativeEnabled,wasOpening=true;
        float previousOpacity=-1;
        public string Stage {get;private set;}="Passthrough · waiting";
        public bool Supported {get;private set;}
        public bool PassthroughActive=>nativeEnabled;
        public bool CameraHdr=>camera.allowHDR;
        public bool CameraPostProcessing=>cameraData.renderPostProcessing;
        public float CameraBackgroundAlpha=>camera.backgroundColor.a;
        public string CompositionProvider=>CompositionLayerManager.Instance?.LayerProvider?.GetType().Name??"None";
        public bool ProjectionLayerReady=>CompositionLayerManager.Instance?.DefaultSceneCompositionLayer!=null;

        public float RoomOpacity {get;private set;}=1;
        public int ParticleCount=>particles?particles.ActiveParticles:0;
        public Transform VisualRoot=>root;
        public PassthroughOpening(ResonantWorld world,ExperienceConfiguration config,Camera camera)
        {
            this.world=world;this.camera=camera;vrColor=camera.backgroundColor;cameraData=camera.GetUniversalAdditionalCameraData();
            root=new GameObject("Passthrough | JERU arrival and portal").transform;
            arrivalAudio=root.gameObject.AddComponent<ArrivalSoundscape>();
            var art=config.finishingModel?config.finishingModel.GetComponent<ShowArtPresentation>():null;
            var source=config.finishingModel?config.finishingModel.GetComponent<CinematicParticleDirector>():null;
            Material line=art?art.filamentMaterial:ResonantGeometry.Material("Arrival electricity",Color.cyan,3);
            for(int k=0;k<bolts.Length;k++){bolts[k]=ResonantGeometry.Line("Sky lightning "+k,root,line,80,k==0?.075f:.023f);bolts[k].enabled=false;bolts[k].startColor=bolts[k].endColor=k==0?Color.white:new Color(.15f,.55f,1);}
            for(int k=0;k<rims.Length;k++){rims[k]=ResonantGeometry.Line("Portal orbit "+k,root,line,80,.04f-k*.005f);rims[k].enabled=false;rims[k].startColor=rims[k].endColor=Color.Lerp(new Color(.2f,.65f,1),new Color(1,.75f,.36f),k/4f);}
            var mesh=portalMesh=new Mesh{name="Portal aperture"};mesh.vertices=new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(-1,1,0),new Vector3(1,1,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();
            portalMaterial=new Material(Shader.Find("LordLyrical/OpeningPortal"));portal=ResonantGeometry.MeshObject("Opening frequency portal",root,mesh,portalMaterial,Vector3.zero,Vector3.one).transform;portal.gameObject.SetActive(false);
            if(source){
                particles=new GameObject("Arrival | cinematic particles").AddComponent<CinematicParticleDirector>();particles.transform.SetParent(root,false);
                particles.sparksMaterial=source.sparksMaterial;particles.plumeMaterial=source.plumeMaterial;particles.energyMaterial=source.energyMaterial;particles.flareMaterial=source.flareMaterial;particles.debrisMaterial=source.debrisMaterial;
                particles.waveMaterial=source.waveMaterial;particles.shieldMaterial=source.shieldMaterial;particles.vortexMaterial=source.vortexMaterial;particles.Initialize(null,null,null);
            }
            Cinema=new CinemaPresentation(world,camera,source);
        }
        static float Noise(int seed){unchecked{uint n=(uint)seed*747796405u+2891336453u;n=((n>>((int)(n>>28)+4))^n)*277803737u;return ((n>>22)^n)/4294967295f;}}
        void Native(bool enabled,float opacity)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!PassthroughFeature.isExtensionEnable)return;
            if(!Supported)Supported=PassthroughFeature.IsPassthroughSupported();
            if(!Supported)return;
            if(enabled!=nativeEnabled){PassthroughFeature.EnableVideoSeeThrough=enabled;if(enabled)PassthroughFeature.PassthroughStart();nativeEnabled=enabled;previousOpacity=-1;}
            if(enabled&&Mathf.Abs(previousOpacity-opacity)>.008f){
                PassthroughFeature.SetPassthroughStyle(new PassthroughStyle{TextureOpacityFactor=opacity,enableColorMap=false,enableEdgeColor=false});previousOpacity=opacity;
            }
#else
            Supported=false;
#endif
        }
        public void Render(double signedSongTime)
        {
            bool waiting=double.IsNegativeInfinity(signedSongTime);
            float t=waiting?-OpeningTiming.Prelude:(float)signedSongTime;
            bool opening=waiting||t<OpeningTiming.VrComplete;
            arrivalAudio.Render(t,waiting);
            bool returning=CinemaTiming.Returning(t);
            Cinema.Render(t,waiting);
            RoomOpacity=returning?1:opening?OpeningTiming.RoomOpacity(t):0;
            Stage=waiting?"Passthrough · waiting":t<OpeningTiming.Arrival?"Passthrough · gathering":t<0?"Passthrough · JERU arrival":t<OpeningTiming.VrStart?"Portal · music running":t<OpeningTiming.VrComplete?"Portal · entering VR":"VR";
            if(returning)Stage=t<CinemaTiming.RealmGone?"Return · releasing the realm":t<CinemaTiming.FounderFadeStart?"Return · floating JERU":t<CinemaTiming.FounderGone?"Return · JERU farewell":t<CinemaTiming.EndTitleGone?"Return · final title":"Passthrough · complete";
            Native(opening||returning,RoomOpacity);
            // Passthrough needs alpha, while opaque VR can use the faster HDR colour format.
            cameraData.renderPostProcessing=!(opening||returning);camera.allowHDR=!(opening||returning);
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=(opening||returning)?new Color(0,0,0,1-RoomOpacity):new Color(vrColor.r,vrColor.g,vrColor.b,1);
            bool reveal=!waiting&&t>=OpeningTiming.Arrival;
            world.SetOpeningPresentation((opening&&t<OpeningTiming.VrStart)||t>=CinemaTiming.RealmGone,reveal&&t<CinemaTiming.FounderGone,t);
            if(!opening){
                if(wasOpening){root.gameObject.SetActive(false);if(particles)particles.Clear();}
                wasOpening=false;return;
            }
            wasOpening=true;root.gameObject.SetActive(true);
            if(particles)particles.EvaluateArrival(t+OpeningTiming.Prelude,new Vector3(0,0,5));
            float lightning=OpeningTiming.Smooth(-2,-1.92f,t)*(1-OpeningTiming.Smooth(-.55f,.15f,t));
            for(int k=0;k<bolts.Length;k++){
                var line=bolts[k];line.enabled=lightning>.001f;if(!line.enabled)continue;
                int pulse=Mathf.FloorToInt((t+2)*17);
                for(int j=0;j<80;j++){
                    float f=j/79f,spread=Mathf.Sin(f*Mathf.PI)*(k==0?.28f:.6f);
                    int seed=pulse*133+j*19+k*941;points[j]=new Vector3((Noise(seed)-.5f)*spread,Mathf.Lerp(19,.3f,f),5+(Noise(seed+7)-.5f)*spread);
                }
                line.SetPositions(points);line.widthMultiplier=(k==0?.075f:.025f)*lightning*(.8f+Noise(pulse+k)*.4f);
            }
            bool gate=t>=0&&t<OpeningTiming.VrComplete;portal.gameObject.SetActive(gate);
            float engulf=OpeningTiming.Smooth(OpeningTiming.EngulfStart,OpeningTiming.VrStart,t);
            float radius=OpeningTiming.PortalRadius(t);
            Vector3 center=OpeningTiming.PortalCenter;
            float fade=1-OpeningTiming.Smooth(7.8f,9,t);
            if(gate){portal.localPosition=center;portal.localScale=Vector3.one*radius;portalMaterial.SetFloat("_Clock",t);portalMaterial.SetFloat("_Opacity",fade);}
            for(int k=0;k<rims.Length;k++){
                var line=rims[k];line.enabled=gate&&fade>.001f&&k<2;if(!line.enabled)continue;
                for(int j=0;j<80;j++){
                    float a=j/79f*6.283185f,r=radius*(1+k*.025f+.009f*Mathf.Sin(a*(7+k)+t*2+k));
                    points[j]=center+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(a*3+t*3+k)*.04f)-Vector3.forward*k*.018f;
                }
                line.SetPositions(points);line.widthMultiplier=(.019f-k*.007f)*fade*(1+engulf);
            }
        }
        static void Release(UnityEngine.Object item){if(!item)return;if(Application.isPlaying)UnityEngine.Object.Destroy(item);else UnityEngine.Object.DestroyImmediate(item);}
        public void Dispose(){Cinema?.Dispose();Native(false,0);if(root)Release(root.gameObject);Release(portalMaterial);Release(portalMesh);}
    }
}
