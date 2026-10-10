using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class Production35Validation
{
    static void Need(bool ok,string why){if(!ok)throw new Exception("PRODUCTION35: "+why);}
    public static void Run(){
        Production34Validation.OutputOverride=Production35Review.Out;
        try{Production34Validation.Run();}finally{Production34Validation.OutputOverride=null;}
        var log=new StringBuilder();Need(TrackSpectrum35.Ready,"Missing track spectrum");
        float variation=0;for(int f=0;f<122*30;f++)for(int band=0;band<24;band++){
            float t=f/30f,v=TrackSpectrum35.Sample(t,band);Need(float.IsFinite(v)&&v>=0&&v<=1,"Invalid spectrum");
            variation+=Mathf.Abs(v-TrackSpectrum35.Sample(t+.1f,band));
        }
        Need(variation>100,"Spectrum frozen");Need(TrackSpectrum35.Sample(-1,4)==0&&TrackSpectrum35.Sample(130,4)==0,"Spectrum outside track");
        for(int f=111*72;f<130*72;f++){
            float t=f/72f;Need(!(CinemaTiming.Title(t)>.001f&&CinemaTiming.Founder(t)<1),"Title behind founder");
            Need(CinemaTiming.Hover(t)>=0&&CinemaTiming.Hover(t)<.34f,"Hover exceeds limit");
        }
        Need(CinemaTiming.Title(119)==0&&CinemaTiming.Title(125)>.99f&&CinemaTiming.Title(130)==0,"Outro ordering");
        Need(CinemaTiming.OutroComplete>=CinemaTiming.EndTitleGone,"Replay cuts off title");
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        SpectralDrift35 drift=null;PassthroughOpening opening=null;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try{
            var cam=new GameObject("Production35 review camera").AddComponent<Camera>();cam.cullingMask=1<<28;cam.nearClipPlane=.03f;cam.fieldOfView=72;cam.backgroundColor=new Color(.025f,.035f,.045f);cam.clearFlags=CameraClearFlags.SolidColor;
            var holder=new GameObject("Waveform inspection");drift=new SpectralDrift35(holder.transform);
            cam.transform.position=SpectralDrift35.Centre(0,40)+new Vector3(0,.035f,-.28f);cam.transform.LookAt(SpectralDrift35.Centre(0,40));
            int maximum=0;
            foreach(float t in new[]{40f,41f,24f,125f,40f}){
                drift.Render(t,true,cam,1);maximum=Math.Max(maximum,drift.VertexCount);Need(drift.VertexCount<=SpectralDrift35.MaxFilaments*SpectralDrift35.Segments*4,"Mesh budget");
                var mesh=holder.GetComponentInChildren<MeshFilter>().sharedMesh;Need(mesh.vertices.All(ManualResonanceBeams.Finite),"Non-finite geometry");
                Layer(scene);ParticipantAvatarBuilder.Render(cam,Production35Review.Out+"/Spectral_Close_"+t+".png",1280,960,1);
            }
            drift.Render(40,true,cam,1);var first=holder.GetComponentInChildren<MeshFilter>().sharedMesh.vertices;
            drift.Render(70,true,cam,1);drift.Render(40,true,cam,1);Need(first.SequenceEqual(holder.GetComponentInChildren<MeshFilter>().sharedMesh.vertices),"Seek changes geometry");
            drift.Render(40,false,cam,1);Need(drift.VertexCount==0&&drift.ActiveFilaments==0,"Paused field visible");
            drift.Dispose();drift=null;UnityEngine.Object.DestroyImmediate(holder);
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Review session").AddComponent<NetworkSessionController>();var world=new GameObject("Review world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            cam.transform.position=new Vector3(0,1.68f,2);cam.transform.rotation=Quaternion.identity;
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(40,-35,0);
            opening=new PassthroughOpening(world,cfg,cam);
            foreach(float t in new[]{40f,59.5f,115f,119f,122.5f,124f,124.5f,125.5f,128f,129.2f}){
                world.Render(ShowFrame.At(Mathf.Clamp(t,0,121)),true);opening.Render(t);Layer(scene);
                Need(opening.Cinema.ActiveParticles<=CinemaPresentation.ParticleBudget,"Outro pool overflow");
                if(t==119)Need(world.FounderRoot.localPosition.y>.27f&&opening.Cinema.TitleOpacity==0,"Founder not floating alone");
                ParticipantAvatarBuilder.Render(cam,Production35Review.Out+"/Spectral_Stage_"+t+".png",1280,960,1);
            }
            world.Render(ShowFrame.At(0),false);opening.Render(double.NegativeInfinity);Need(world.FounderRoot.localPosition.y<.01f,"Replay retains levitation");
            var shader=Resources.Load<Shader>("Cinema26/SpectralThread35");Need(shader&&shader.isSupported&&!ShaderUtil.GetShaderMessages(shader).Any(x=>x.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Spectral shader");
            log.AppendLine("PASS 87,840 spectrum samples bounded; measured variation="+variation+"; silence outside track.");
            log.AppendLine("PASS spectral mesh capacity, finite vertices, exact backward seek, clear while inactive; sampled max vertices="+maximum);
            log.AppendLine("PASS no title/founder overlap, 30cm levitation, return before farewell before title, replay after digital dissolve, particle capacity and shader checks.");
            log.AppendLine("Render backgrounds are editor fixtures, not headset passthrough footage. Native frame rate and physical multi-headset acceptance remain pending.");
            File.WriteAllText(Production35Review.Out+"/Spectral35_Validation.txt",log.ToString());Debug.Log("PRODUCTION35_SPECTRAL_PASS\n"+log);
        }finally{drift?.Dispose();opening?.Dispose();EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
    }
    static void Layer(Scene scene){foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;}
}
