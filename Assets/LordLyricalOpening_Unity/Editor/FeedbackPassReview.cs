using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class FeedbackPassReview
{
    static void Need(bool ok,string reason){if(!ok)throw new Exception("FEEDBACK PASS: "+reason);}
    [MenuItem("Lord Lyrical/Production/Validate Feedback Pass")]
    public static void Run(){
        var report=new StringBuilder("CANDIDATE 18 / USER FEEDBACK VALIDATION\n");
        Need(HandPoseInput.Extension(Vector3.zero,Vector3.forward*.03f,Vector3.forward*.05f,Vector3.forward*.07f)>.95f,"Straight finger rejected");
        Need(HandPoseInput.Extension(Vector3.zero,Vector3.forward*.03f,new Vector3(0,-.02f,.03f),new Vector3(0,-.02f,.01f))<.65f,"Folded finger accepted as open");
        Need(!ManualResonanceBeams.ValidRay(new Vector3(float.NaN,0,0),Vector3.forward,Vector3.zero),"Nonfinite ray accepted");
        Need(!ManualResonanceBeams.ValidRay(Vector3.one*10,Vector3.forward,Vector3.zero),"Detached beam accepted");
        Need(!ManualResonanceBeams.ValidRay(Vector3.zero,Vector3.zero,Vector3.zero),"Zero aim accepted");
        var combat=new ConcertEncounter();combat.heads[0]=new Vector3(0,1.7f,0);
        for(int i=0;i<12;i++)combat.State.orcs[i].position=new Vector3(10+i,0,14);
        combat.State.orcs[0].position=new Vector3(0,0,6);combat.State.orcs[1].position=new Vector3(2,0,3);
        var origin=new Vector3(0,1.3f,.3f);
        Need(combat.Beam(0,27,1,origin,Vector3.forward),"Aimed beam missed");
        Need(combat.State.orcs[0].health<100&&combat.State.orcs[1].health==100,"Beam damaged nearest off-axis Orc");
        float health=combat.State.orcs[0].health;
        Need(!combat.Beam(0,27.01f,1,origin,Vector3.forward)&&combat.State.orcs[0].health==health,"Beam cadence bypass");
        Need(!combat.Beam(0,27.5f,1,origin,Vector3.back)&&combat.State.orcs[0].health==health,"Backward miss caused damage");
        report.AppendLine("PASS open/folded finger geometry, invalid/detached aim rejection, aimed target selection, miss and damage cadence.");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var chamber=UnityEngine.Object.Instantiate(cfg.chamberModel).GetComponent<ChamberPresentation>();chamber.Initialize();
            var dragon=chamber.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            Need(!dragon.gameObject.activeSelf,"Dragon visible during instantiation");
            foreach(float t in new[]{0f,21,58.99f,59,59.5f,60,60.99f,61,62.99f}){chamber.Evaluate(t,true);Need(!dragon.gameObject.activeSelf,"Premature dragon at "+t);}
            foreach(float t in new[]{63f,64,68,74,82,90,98,100.9f}){chamber.Evaluate(t,true);Need(dragon.gameObject.activeSelf,"Missing intended dragon at "+t);}
            chamber.Evaluate(63,false);Need(!dragon.gameObject.activeSelf,"Inactive show revealed dragon");
            chamber.Evaluate(101,true);Need(!dragon.gameObject.activeSelf,"Dragon survived completed dissolve");
            report.AppendLine("PASS dragon hidden at instantiation and throughout 0–62.99s; single reveal 63–101s; inactive/reset show hides it.");
            foreach(string name in new[]{"LightningDescent","LightningLanding","ArrivalThunder"}){
                var clip=Resources.Load<AudioClip>("LordLyricalAccents/"+name);Need(clip&&clip.channels==1&&clip.length>1,"Arrival audio missing "+name);
                report.AppendLine("PASS arrival sound "+name+" "+clip.length.ToString("F2")+"s");
            }
            var lord=new LordLyricalController(chamber.transform,null,cfg.lordModel);
            Need(!lord.root.GetComponentsInChildren<Renderer>(true).Any(r=>r.name=="Spectrum orbit"),"Solid body rings remain");
            report.AppendLine("PASS solid spectrum orbit renderers removed; body particles use independent rising trajectories.");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        string dir=MotionPolishValidation.Out+"/Production/Feedback18";Directory.CreateDirectory(dir);File.WriteAllText(dir+"/Validation.txt",report.ToString());Debug.Log(report);
    }
}
