using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class Flow28Review
{
    const string Out=MotionPolishValidation.Out+"/Production/Flow28";
    static void Need(bool ok,string why){if(!ok)throw new Exception("FLOW28: "+why);}
    [MenuItem("Lord Lyrical/Flow28/Validate and Render")]
    public static void Validate(){
        Directory.CreateDirectory(Out);var log=new StringBuilder();
        Need(Mathf.Abs(OpeningTiming.Arrival-CinemaTiming.IntroTitleGone-3)<.0001f,"Intro gap");
        Need(Mathf.Abs(CinemaTiming.FounderFadeStart-CinemaTiming.EndTitleGone-3)<.0001f,"Outro gap");
        for(float t=-5;t<-2;t+=.01f)Need(CinemaTiming.Title(t)==0&&OpeningTiming.JeruPosition(t).y>=11.99f,"Intro overlap");
        for(float t=122;t<=125;t+=.01f)Need(CinemaTiming.Title(t)==0&&CinemaTiming.Founder(t)==0,"Outro overlap");
        Need(!PalmBeamTiming.Allowed(9)&&!PalmBeamTiming.Allowed(21.34f)&&PalmBeamTiming.Allowed(21.35f)&&!PalmBeamTiming.Allowed(111),"Palm gate");
        Need(PalmBeamTiming.Teaching(PalmBeamTiming.Unlock),"No instruction at beam unlock");
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        var cues=JsonUtility.FromJson<CueTable>(config.cueJson.text).cues;
        var scheduler=new CueScheduler(cues);int fired=0;double sample=0,maxError=0;scheduler.Fired+=c=>{fired++;maxError=Math.Max(maxError,sample-c.time);};
        for(int i=0;i<=121*72;i++){sample=i/72d;scheduler.Advance(sample);}
        Need(fired==cues.Length&&maxError<1d/72+.00001,"Missing/late narrative cue");
        var timeline=new CueTimeline(cues);foreach(var cue in cues)Need(Math.Abs(timeline.NarrativeTime(cue.time)-cue.authoredTime)<.00001,"Retimed cue mismatch "+cue.id);
        foreach(var pair in new[]{("dragon-reveal",DragonRevealScore.Start),("wings-open",DragonRevealScore.Wings),("eyes-open",DragonRevealScore.Eyes),("dragon-roar",DragonRevealScore.Roar),("breath-beam",DragonRevealScore.Attack)})Need(Math.Abs(cues.Single(c=>c.id==pair.Item1).time-pair.Item2)<.00001,"Reveal cue mismatch");
        Need(cues.Single(c=>c.id=="title-assembly").time==CinemaTiming.EndTitleStart,"Title misses script cue");
        log.AppendLine($"PASS {fired} musical cues exactly once at 72Hz; maximum scheduler lateness {maxError*1000:F3}ms; cue-to-narrative mapping intact.");
        log.AppendLine("PASS exact three-second title gaps; palm unlock after instruction fade-in; reveal phases match authored break markers.");
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);PassthroughOpening opening=null;
        bool async=ShaderUtil.allowAsyncCompilation;
        try{
            ShaderUtil.allowAsyncCompilation=false;
            var session=new GameObject("Flow28 review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Flow28 review world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var cam=new GameObject("Flow28 review camera").AddComponent<Camera>();cam.transform.position=new Vector3(0,1.68f,2);cam.fieldOfView=85;cam.nearClipPlane=.08f;cam.farClipPlane=100;cam.cullingMask=1<<28;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.03f,.04f,.06f);
            opening=new PassthroughOpening(world,config,cam);
            var key=new GameObject("Review key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(35,-25,0);key.cullingMask=1<<28;
            foreach(float t in new[]{-7f,-6f,-5f,-3.5f,-2f,63f,64.8f,65.4f,66.6f,68.5f,111f,114f,121f,122f,124.9f,126f,127.5f}){
                bool dragon=t>=63&&t<69;cam.transform.position=dragon?new Vector3(0,2.7f,0):new Vector3(0,1.68f,2);cam.transform.LookAt(dragon?new Vector3(0,6.5f,12):new Vector3(0,2.1f,5));
                world.Render(ShowFrame.At(Mathf.Clamp(t,0,121)),t>=0);opening.Render(t);
                foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                if(t==122||t==124.9f){Need(opening.Cinema.TitleOpacity==0,"End title remains");Need(world.FounderRoot.GetComponentsInChildren<Renderer>(true).Any(r=>!r.forceRenderingOff),"Founder hidden during pause");}
                if(t>=127.5f)Need(world.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeInHierarchy).All(r=>r.forceRenderingOff),"Founder remains after farewell");
                ParticipantAvatarBuilder.Render(cam,Out+"/Flow_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
            }
            Need(world.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="Current CyberDragon | chamber performance")==1,"Duplicate dragon");
            File.WriteAllLines(Out+"/Dragon_Renderers.txt",world.GetComponentInChildren<ChamberPresentation>().GetComponentsInChildren<Renderer>(true).Where(r=>!r.name.StartsWith("CH_")).Select(r=>r.name));
            world.Render(ShowFrame.At(0),false);opening.Render(double.NegativeInfinity);
            Need(opening.Cinema.RealmRelease==0&&opening.Cinema.TitleOpacity>0,"Restart fails");
            var shader=Resources.Load<Shader>("Cinema26/Title");Need(shader&&shader.isSupported,"Title shader unsupported");Need(!ShaderUtil.GetShaderMessages(shader).Any(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Title shader errors");
            log.AppendLine("PASS actual world/title/founder lifecycle beyond song end, seek reset, one preserved dragon, title shader compilation.");
        }finally{opening?.Dispose();EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
        Performance25Review.EvidenceOverride=Out;
        try{Performance25Review.Duel();Performance25Review.Beams();}finally{Performance25Review.EvidenceOverride=null;}
        File.WriteAllText(Out+"/Flow28_Validation.txt",log.ToString());Debug.Log("FLOW28_PASS\n"+log);
    }
    [MenuItem("Lord Lyrical/Flow28/Prepare")]
    public static void Prepare(){PlayerSettings.bundleVersion="1.0.28";PlayerSettings.Android.bundleVersionCode=28;LordLyrical.Editor.ShowBuilder.ConfigureAndroid();AssetDatabase.SaveAssets();}
    public static void BuildAll(){EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);Validate();Prepare();File.WriteAllText(Out+"/Build_Started.json","{\"started\":"+DateTimeOffset.UtcNow.ToUnixTimeSeconds()+"}");LordLyrical.Editor.ShowBuilder.BuildPico();}
}
