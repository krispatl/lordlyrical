using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class Production42Review
{
    public const string Out=MotionPolishValidation.Out+"/Production/Production42";
    static void Need(bool ok,string why){if(!ok)throw new Exception("PRODUCTION38: "+why);}
    public static void Validate()
    {
        Directory.CreateDirectory(Out);var log=new StringBuilder();
        var audio=Resources.Load<AudioClip>("Cinema26/TitleDissolve30");Need(audio&&Mathf.Abs(audio.length-2)<.02f,"Title dissolve sound missing or wrong duration");
        // Check real encounter logic: target lock, dodge, guard, no repeated damage and reset.
        for(int mode=0;mode<3;mode++){
            var e=new ConcertEncounter();e.occupied[0]=true;e.heads[0]=new Vector3(0,1.65f,2);e.DragonMouth=new Vector3(0,7,13);
            for(int f=81*72;f<92*72;f++){
                float t=f/72f;if(mode==1)e.guards[0]=t+.4f;
                if(mode==2){float start=DragonFireballScore.Launch(0);e.heads[0]=new Vector3(t>start?3:0,1.65f,2);}
                e.Tick(t,1f/72);
            }
            Need(e.State.dragonShots.All(s=>s.armed&&s.launched&&s.resolved),"Missed shot");
            Need(mode!=0||e.State.hits==3,"Fireball hits counted wrong");
            Need(mode!=1||(e.State.blocks==3&&e.State.hits==0),"Guard failure");
            Need(mode!=2||e.State.hits==2,"Target followed player after launch");
            int hits=e.State.hits;e.Tick(93,.01f);Need(e.State.hits==hits,"Repeated hit");
            string json=JsonUtility.ToJson(e.State);Need(json.Length<20000,"Snapshot too large");Need(JsonUtility.FromJson<ConcertState>(json).dragonShots.All(s=>s.resolved),"Snapshot lost shots");
            e.Reset(2,0);Need(e.State.dragonShots.All(s=>!s.armed),"Restart retains fireball");
        }
        var empty=new ConcertEncounter();for(int f=81*72;f<92*72;f++)empty.Tick(f/72f,1f/72);Need(empty.State.dragonShots.All(s=>!s.armed),"Absent players targeted");
        log.AppendLine("PASS authority target lock, 3 hits once, 3 guards, dodge, absent player, JSON snapshot and reset.");
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try{
            var session=new GameObject("Production32 session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Production32 world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var cam=new GameObject("Production32 camera").AddComponent<Camera>();cam.transform.position=new Vector3(0,1.7f,2);cam.transform.LookAt(new Vector3(0,5.7f,13));cam.fieldOfView=85;cam.nearClipPlane=.08f;cam.farClipPlane=100;cam.cullingMask=1<<28;cam.backgroundColor=new Color(.03f,.04f,.06f);
            var key=new GameObject("Review light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(35,-25,0);key.cullingMask=1<<28;
            var checkChamber=world.GetComponentInChildren<ChamberPresentation>();
            Vector3 previous=Vector3.zero;float biggest=0,worstTime=0;
            for(int f=63*72;f<69*72;f++){
                float sample=f/72f;checkChamber.Evaluate(sample,true);
                Need(ManualResonanceBeams.Finite(checkChamber.Mouth),"Non-finite reveal pose");
                if(f>63*72){float step=Vector3.Distance(previous,checkChamber.Mouth);if(step>biggest){biggest=step;worstTime=sample;}}previous=checkChamber.Mouth;
            }
            Need(biggest<.4f,"Reveal pose snap "+biggest+" at "+worstTime);log.AppendLine("PASS reveal continuity at 72Hz; largest mouth step "+biggest.ToString("F4")+"m at "+worstTime);
            foreach(float t in new[]{62.9f,63.3f,63.8f,64.3f,64.8f,65.4f,66.6f,68.5f,70f,71.45f,72.01f,75f,76.55f,82.5f,83.4f,84.35f,85.48f,90.45f,95f,98.22f,101f}){
                world.Render(ShowFrame.At(t),true);
                var chamber=world.GetComponentInChildren<ChamberPresentation>();
                var combat=world.GetComponent<ConcertInteraction>().Encounter;
                if(t>=81&&t<91){var shot=combat.State.dragonShots[0];shot.armed=true;shot.launched=t>=82.8f;shot.slot=0;shot.target=new Vector3(1.2f,1.2f,3);shot.origin=chamber.Mouth;
                    chamber.Battle=combat.State;chamber.Evaluate(t,true);world.GetComponentInChildren<DragonFireballVFX>().Render(t,true,chamber.Mouth,combat.State,1);}
                var head=chamber.GetComponentsInChildren<Transform>(true).First(x=>x.name=="head");
                log.AppendLine($"t={t:F2} head={head.position:F3} mouth={chamber.Mouth:F3} aim={chamber.MouthForward:F3}");
                foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                ParticipantAvatarBuilder.Render(cam,Out+"/Dragon_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
                if(t==75){
                    var position=cam.transform.position;var rotation=cam.transform.rotation;
                    cam.transform.position=new Vector3(-12,5,10);cam.transform.LookAt(new Vector3(0,7.5f,14));
                    ParticipantAvatarBuilder.Render(cam,Out+"/Dragon_Profile.png",1280,960,1);
                    cam.transform.SetPositionAndRotation(position,rotation);
                }
                Need(world.GetComponentInChildren<DragonFireballVFX>().ActiveParticles<=DragonFireballVFX.Budget,"VFX pool exceeded");
            }
            Need(world.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="Current CyberDragon | chamber performance")==1,"Duplicate dragon");
            var shader=Shader.Find("LordLyrical/Resonant Actor Surface");Need(!ShaderUtil.GetShaderMessages(shader).Any(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Breach shader failed");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
        File.WriteAllText(Out+"/Production38_Validation.txt",log.ToString());Debug.Log("PRODUCTION38_PASS\n"+log);
    }
    public static void BuildFinal(){
        HandTracking42Regression.Run();Presentation42Validation.Run();ImpactTransition42Validation.Run();Presence42Validation.Run();
        PlayerSettings.bundleVersion="1.0.42";PlayerSettings.Android.bundleVersionCode=42;AssetDatabase.SaveAssets();File.WriteAllText(Out+"/Build_Started.txt",DateTime.UtcNow.ToString("O"));LordLyrical.Editor.ShowBuilder.BuildPico();OfflineDemoBuilder.Build();
    }
    public static void BuildAll(){
        Validate();Production42Regression.Run();Production42CombatRegression.Run();HandTracking42Regression.Run();Presentation42Validation.Run();ImpactTransition42Validation.Run();Presence42Validation.Run();Performance25Review.EvidenceOverride=Out;try{Performance25Review.Performance();}finally{Performance25Review.EvidenceOverride=null;}JeruGestureValidation.Run();Performance25Review.EvidenceOverride=Out;
        try{Performance25Review.Duel();Performance25Review.Beams();}finally{Performance25Review.EvidenceOverride=null;}
        Need(CinemaTiming.EndTitleStart>CinemaTiming.FounderGone&&OpeningTiming.Arrival-CinemaTiming.IntroTitleGone==3,"Title timing changed");
        Need(!PalmBeamTiming.Allowed(21.34f)&&PalmBeamTiming.Allowed(21.35f),"Palm gate changed");
        PlayerSettings.bundleVersion="1.0.42";PlayerSettings.Android.bundleVersionCode=42;AssetDatabase.SaveAssets();File.WriteAllText(Out+"/Build_Started.txt",DateTime.UtcNow.ToString("O"));LordLyrical.Editor.ShowBuilder.BuildPico();OfflineDemoBuilder.Build();
    }
}


