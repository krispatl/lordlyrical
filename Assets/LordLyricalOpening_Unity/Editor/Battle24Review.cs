using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;
using LordLyrical;
public static class Battle24Review
{
    const string Out=MotionPolishValidation.Out+"/Production/Battle24";
    [MenuItem("Lord Lyrical/Battle24/Audit Pursuit")]
    public static void Audit()
    {
        Directory.CreateDirectory(Out);var log=new StringBuilder();
        var nav=NavMesh.AddNavMeshData(Resources.Load<NavMeshData>("Concert/ChamberNavigation"));
        try{
            foreach(float z in new[]{2f,0f,-2f}){
                var c=new ConcertEncounter();c.occupied[0]=true;c.heads[0]=new Vector3(0,1.65f,z);
                for(int k=0;k<24*60;k++){
                    float t=26+k/60f;c.Tick(t,1/60f);
                    if(k%360==0||k==24*60-1){
                        log.AppendLine($"t={t:F2}, headZ={z}, hits={c.State.hits}");
                        for(int j=0;j<12;j++){var o=c.State.orcs[j];log.AppendLine($"  {j}: {o.intent}, position={o.position:F2}, speed={o.velocity.magnitude:F2}, range={Vector3.Distance(new Vector3(o.position.x,0,o.position.z),new Vector3(0,0,z)):F2}");}
                    }
                }
            }
            File.WriteAllText(Out+"/Pursuit_Audit.txt",log.ToString());Debug.Log("BATTLE24_PURSUIT_AUDIT "+Out);
        }finally{if(nav.valid)nav.Remove();}
    }
    static void Need(bool ok,string reason){if(!ok)throw new Exception("BATTLE24: "+reason);}
    static float Range(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    [MenuItem("Lord Lyrical/Battle24/Verify Fixes")]
    public static void Verify()
    {
        Directory.CreateDirectory(Out);var log=new StringBuilder("BATTLE24 TARGETED REGRESSION VALIDATION\n");
        var nav=NavMesh.AddNavMeshData(Resources.Load<NavMeshData>("Concert/ChamberNavigation"));
        try{
            foreach(float z in new[]{2f,0f,-2f}){
                var c=new ConcertEncounter();c.occupied[0]=true;c.heads[0]=new Vector3(0,1.65f,z);
                float min=99;var closest=Enumerable.Repeat(99f,12).ToArray();var watching=new float[12];
                for(int k=0;k<24*60;k++){
                    float t=26+k/60f;c.Tick(t,1/60f);
                    for(int j=0;j<12;j++){
                        float r=Range(c.State.orcs[j].position,c.heads[0]);min=Mathf.Min(min,r);closest[j]=Mathf.Min(closest[j],r);
                        watching[j]=c.State.orcs[j].intent==OrcIntent.Watching?watching[j]+1/60f:0;
                        if(t>32)Need(watching[j]<.25f,"Orc "+j+" forgot a present participant at "+t);
                    }
                }
                Need(c.State.hits>0,"Pursuit did not reach attack range");
                Need(closest.All(r=>r<3.3f),"An Orc remained behind the stage");
                Need(min>=1.298f,"Navigation projection broke personal space");
                log.AppendLine($"PASS headZ={z}: all 12 Orcs approach within 3.3m, {c.State.hits} landed attacks, minimum separation {min:F3}m; no target loss.");
            }
            {
                var c=new ConcertEncounter();c.occupied[0]=true;c.heads[0]=new Vector3(0,1.65f,2);
                var watching=new float[12];
                for(int k=0;k<18*60;k++){
                    float t=26+k/60f;
                    if(t>=34)c.heads[0]=new Vector3(Mathf.Lerp(0,4,Mathf.Clamp01((t-34)/4)),1.65f,Mathf.Lerp(2,-1,Mathf.Clamp01((t-34)/4)));
                    c.Tick(t,1/60f);
                    for(int j=0;j<12;j++){watching[j]=c.State.orcs[j].intent==OrcIntent.Watching?watching[j]+1/60f:0;if(t>32)Need(watching[j]<.25f,"Moving participant was forgotten");}
                }
                Need(c.State.orcs.All(o=>Range(o.position,c.heads[0])<4),"Orcs did not follow the moving participant");
                c.occupied[0]=false;c.occupied[1]=true;c.heads[1]=new Vector3(-4,1.65f,0);
                for(int k=0;k<180;k++)c.Tick(44+k/60f,1/60f);
                Need(c.State.orcs.All(o=>o.target==1),"Disconnected target was retained");
                c.occupied[1]=false;
                for(int k=0;k<180;k++)c.Tick(47+k/60f,1/60f);
                Need(c.State.orcs.All(o=>o.intent==OrcIntent.Watching),"Empty room still pursued a phantom participant");
                log.AppendLine("PASS moving head pursuit, participant disconnect/reassignment, and no phantom target in an empty room.");
            }
        }finally{if(nav.valid)nav.Remove();}
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Battle24 review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Battle24 review world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            var chamber=world.GetComponentInChildren<ChamberPresentation>();var cinema=world.GetComponentInChildren<CinematicParticleDirector>();
            var dragon=world.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            float previous=0;Vector3 stable=Vector3.zero;
            foreach(float charge in new[]{0f,1f}){
                chamber.Battle=new ConcertState{finaleCharge=charge};
                foreach(float t in new[]{97.9f,98f,98.2f,99.5f,100.9f,101f,105f,120f}){
                    chamber.Evaluate(t,true);
                    Need(dragon.gameObject.activeSelf==(t<101),"Dragon visibility depends on charge or runs past destruction cue");
                    if(t>=98){if(t==98){stable=chamber.Heart;previous=0;}Need(Vector3.Distance(stable,chamber.Heart)<.0001f,"Impact origin moved after musical hit");}
                    if(t<101){
                        var m=dragon.GetComponentsInChildren<Renderer>().First().sharedMaterial;
                        float dissolve=m.GetFloat("_Dissolve");
                        if(t>=98){Need(dissolve>=previous,"Dissolve reversed");previous=dissolve;}
                        if(t==100.9f)Need(dissolve>.995f,"Dragon is not fully dissolved before hiding");
                    }
                }
            }
            chamber.Evaluate(98,true);var continuousImpact=chamber.Heart;
            chamber.Evaluate(105,true);Need(Vector3.Distance(continuousImpact,chamber.Heart)<.0001f,"Seek changed impact anchor");
            var camera=new GameObject("Battle24 review camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=76;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.transform.position=new Vector3(-5,2,1);camera.transform.LookAt(new Vector3(0,5.5f,11));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.012f,.02f);CinematicLook.Configure(camera);
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(.7f,.82f,1);light.transform.rotation=Quaternion.Euler(35,-30,0);light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.13f,.15f,.19f);
            foreach(float t in new[]{97.9f,98.15f,99.3f,100.6f,101.05f,105f}){
                world.Render(ShowFrame.At(t),true);
                foreach(var tr in world.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                Need(cinema.AllFinite(),"Nonfinite ending particles");Need(cinema.DroppedParticles==0,"Ending particles overflowed");
                ParticipantAvatarBuilder.Render(camera,Out+"/Dragon_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",960,720,1);
            }
            log.AppendLine("PASS zero/full contribution: destruction starts at 98s, almost fully dissolved at 100.9s, inactive at 101s through finale; fixed impact anchor, finite effects and no particle drops.");
            var actors=(OrcEncounterController)typeof(ResonantWorld).GetField("orcs",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(world);
            var chase=new ConcertEncounter();chase.occupied[0]=true;chase.heads[0]=new Vector3(0,1.65f,2);
            for(int k=0;k<18*60;k++)chase.Tick(26+k/60f,1/60f);
            world.Render(ShowFrame.At(44),true);actors.RenderInteractive(chase.State,44);
            camera.transform.position=new Vector3(-5,4,-3);camera.transform.LookAt(new Vector3(0,1.3f,3));
            foreach(var tr in world.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
            ParticipantAvatarBuilder.Render(camera,Out+"/Pursuit_44s.png",960,720,1);
            log.AppendLine("PASS pursuit visual staged using the simulated authoritative state in the actual chamber. Native wearer acceptance pending.");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
        File.WriteAllText(Out+"/Fix_Validation.txt",log.ToString());Debug.Log("BATTLE24_FIXES_PASS\n"+log);
    }

}
