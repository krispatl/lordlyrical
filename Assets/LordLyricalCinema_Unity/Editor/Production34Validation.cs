using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;
public static class Production34Validation
{
    public static string OutputOverride;
    static string Out=>OutputOverride??Production34Review.Out;
    static void Need(bool value,string why){if(!value)throw new Exception("PRODUCTION34: "+why);}
    public static void Run()
    {
        Magic33Validation.OutputOverride=Out;Production32CombatValidation.OutputOverride=Out;
        try{Magic33Validation.Run();Production32CombatValidation.Run();}finally{Magic33Validation.OutputOverride=null;Production32CombatValidation.OutputOverride=null;}
        var shot=new DragonShot{armed=true,target=new Vector3(0,1.2f,3),origin=new Vector3(0,8,12)};
        float launch=DragonFireballScore.Launch(0);
        Need(Combat34Score.FireballWarning(launch-2,0,shot)==0,"Early warning");
        Need(Combat34Score.FireballWarning(launch-.3f,0,shot)>0,"Missing warning");
        Need(Combat34Score.FireballWarning(launch+DragonFireballScore.Flight,0,shot)==0,"Warning after impact");
        shot.resolved=true;Need(Combat34Score.FireballWarning(launch,0,shot)==0,"Resolved warning remains");
        foreach(OrcAttack attack in Enum.GetValues(typeof(OrcAttack))){
            var orc=new ConcertOrc{health=100,intent=OrcIntent.Telegraph,attack=attack,attackStarted=30};
            Need(Combat34Score.OrcWarning(orc,30)<Combat34Score.OrcWarning(orc,30+OrcCombatMotion.Windup(attack)),"Windup lacks progression");
            orc.health=0;Need(Combat34Score.OrcWarning(orc,31)==0,"Dead enemy has warning");
        }
        Need((JeruReferenceScore.LivingRest(0,10).position-JeruReferenceScore.LivingRest(0,12).position).magnitude>.001f,"Idle frozen");
        for(int i=0;i<121*36;i++)for(int side=0;side<2;side++)Need(Vector3.Distance(JeruReferenceScore.LivingRest(side,i/36f).position,JeruReferenceScore.Rest(side).position)<.01f,"Idle displacement too large");
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try{
            var camera=new GameObject("Combat readability camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,1.7f,2);camera.fieldOfView=82;camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.055f,.075f);
            var fire=new GameObject("Fireball review").AddComponent<DragonFireballVFX>();fire.Initialize();var state=new ConcertState();
            int maximum=0;
            for(int frame=81*72;frame<93*72;frame++){
                float t=frame/72f;
                for(int i=0;i<3;i++){var s=state.dragonShots[i];s.armed=t>=DragonFireballScore.Launch(i)-DragonFireballScore.Windup;s.launched=t>=DragonFireballScore.Launch(i);s.resolved=t>=DragonFireballScore.Launch(i)+DragonFireballScore.Flight;s.blocked=i==0;s.target=new Vector3((i-1)*1.5f,1.2f,4);s.origin=new Vector3(0,7,13);}
                fire.Render(t,true,new Vector3(0,7,13),state,1);maximum=Math.Max(maximum,fire.ActiveParticles);Need(fire.ActiveParticles<=DragonFireballVFX.Budget,"Fireball pool overflow");
            }
            state.dragonShots[0]=new DragonShot{armed=true,launched=false,origin=new Vector3(0,7,13),target=new Vector3(0,1.2f,4),slot=0};
            fire.Render(launch-.2f,true,new Vector3(0,7,13),state,1);Need(fire.WarningParticles>0,"Missing target footprint");Layer(scene);camera.transform.LookAt(new Vector3(0,.2f,4));ParticipantAvatarBuilder.Render(camera,Out+"/Fireball_Warning.png",1000,800,1);
            state.dragonShots[0].launched=state.dragonShots[0].resolved=state.dragonShots[0].blocked=true;
            fire.Render(launch+DragonFireballScore.Flight+.18f,true,new Vector3(0,7,13),state,1);Need(fire.GuardParticles>0,"Missing shield breakup");camera.transform.LookAt(new Vector3(0,1.2f,4));ParticipantAvatarBuilder.Render(camera,Out+"/Fireball_Guarded.png",1000,800,1);
            fire.Render(90,false,Vector3.zero,state,1);Need(fire.ActiveParticles==0,"Hidden fireballs persist");
            var guard=new GameObject("Guard review").AddComponent<GuardContactPresentation>();guard.Initialize();guard.Trigger(new Vector3(0,1.3f,3),Vector3.forward,30);guard.Render(30.12f,true);Need(guard.ActiveParticles==96,"Guard missing");Layer(scene);camera.transform.LookAt(new Vector3(0,1.3f,3));ParticipantAvatarBuilder.Render(camera,Out+"/Hand_Guard.png",1000,800,1);
            guard.Render(30.7f,true);Need(guard.ActiveParticles==0,"Guard persists");guard.Trigger(Vector3.zero,Vector3.up,31);guard.Render(31.1f,false);Need(guard.ActiveParticles==0,"Unfocused guard remains");guard.Clear();guard.Render(31.1f,true);Need(guard.ActiveParticles==0,"Replay guard remains");
            var label=new GameObject("Rescue card").AddComponent<TextMesh>();label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.text="LEND JERU YOUR LIGHT\nPALMS TOWARD HIS SHIELD";
            var card=label.gameObject.AddComponent<SpatialInstructionCard>();card.Initialize(label);card.Progress(.65f);card.Appearance(Color.cyan,1,true);label.transform.position=new Vector3(0,1.25f,4);label.transform.localScale=Vector3.one*1.5f;camera.transform.LookAt(label.transform.position);Layer(scene);ParticipantAvatarBuilder.Render(camera,Out+"/Rescue_Card.png",1000,800,1);
            Need(!ShaderUtil.GetShaderMessages(Resources.Load<Shader>("Cinema26/InstructionCard")).Any(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Card shader error");
            File.WriteAllText(Out+"/Readability34_Validation.txt","PASS fireball warning lifecycle; five Orc windups; 8712 bounded idle samples; 864 fireball frames (max "+maximum+"/720); blocked breakup; guard expiry/focus/reset; cue-card shader and editor renders.\nSynthetic fixtures, not native performance acceptance.\n");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
        Debug.Log("PRODUCTION34_READABILITY_PASS");
    }
    static void Layer(Scene scene){foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;}
}
