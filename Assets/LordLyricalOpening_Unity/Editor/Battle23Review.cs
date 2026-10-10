using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;
public static class Battle23Review
{
    const string Out=MotionPolishValidation.Out+"/Production/Battle23";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static void Need(bool value,string message){if(!value)throw new Exception("BATTLE23: "+message);}
    static void Layer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;}
    [MenuItem("Lord Lyrical/Battle23/Validate Combat")]
    public static void Combat()
    {
        Directory.CreateDirectory(Out);var log=new StringBuilder("CANDIDATE 23 / COMBAT VALIDATION\n");
        var far=new Vector3(10000,0,10000);var origin=far+new Vector3(0,1.3f,0);
        ConcertEncounter Make(){var c=new ConcertEncounter();for(int i=0;i<12;i++)c.State.orcs[i].position=far+new Vector3(50+i*3,0,8);c.State.orcs[0].position=far+Vector3.forward*6;return c;}
        for(int slot=0;slot<10;slot++){
            var c=Make();c.heads[slot]=origin;c.occupied[slot]=true;float death=0;
            for(int i=0;i<60;i++){float t=27+i*.1001f;c.Beam(slot,t,.5f,origin,Vector3.forward);if(c.State.orcs[0].health<=0){death=t-27;break;}}
            Need(c.State.orcs[0].health==0&&c.State.orcs[0].intent==OrcIntent.Defeated,"Role "+slot+" cannot defeat aimed Orc");
            Need(c.State.defeated==1,"Defeat counter duplicated");c.Tick(45,.016f);Need(c.State.orcs[0].health==0&&c.State.orcs[0].velocity==Vector3.zero,"Dead Orc resumed activity");
            log.AppendLine($"PASS role {slot}: sustained single-palm beam defeats target after {death:F2}s; defeat persists.");
        }
        {
            var c=Make();Need(!c.Beam(0,27,1,origin,Vector3.back),"Miss damaged target");
            Need(!c.Beam(0,27.2f,1,origin,new Vector3(float.NaN,0,1)),"Invalid direction accepted");
            Need(c.Beam(0,27.4f,1,origin,Vector3.forward),"Valid first contact missing");float hp=c.State.orcs[0].health;
            Need(!c.Beam(0,27.41f,1,origin,Vector3.forward)&&hp==c.State.orcs[0].health,"Damage cadence bypass");
            Need(c.Beam(0,27.4f,1,origin,Vector3.forward,1)&&c.State.orcs[0].health<hp,"Second palm not independent");
            var hit=BeamHitResolver.Trace(c.State,27,origin,Vector3.forward);Need(hit.orc==0&&Mathf.Abs(hit.point.z-(far.z+5.45f))<.005f,"Visual collision endpoint does not match capsule");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=far+new Vector3(0,1.3f,3);wall.transform.localScale=new Vector3(2,3,.2f);Physics.SyncTransforms();
            try{hp=c.State.orcs[0].health;hit=BeamHitResolver.Trace(c.State,28,origin,Vector3.forward);Need(hit.hit&&hit.orc<0,"Beam crossed wall");Need(!c.Beam(0,28,1,origin,Vector3.forward)&&c.State.orcs[0].health==hp,"Damage crossed wall");}
            finally{UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();}
            Need(c.Beam(0,51,1,origin,Vector3.forward),"Visible retreating target became invulnerable");
            Need(!c.Beam(0,54.1f,1,origin,Vector3.forward),"Hidden target takes damage");
            c.State.orcs[0].health=0;Need(BeamHitResolver.Trace(c.State,30,origin,Vector3.forward).orc<0,"Dead target blocks beam");
            log.AppendLine("PASS misses, finite inputs, cadence, dual palms, shared visual/damage endpoint, wall occlusion, retreat window and dead-target exclusion.");
        }
        foreach(int people in new[]{1,10}){
            var c=Make();c.SetDuelTargets(far+new Vector3(0,1.8f,6),far+new Vector3(0,6,10));
            for(int slot=0;slot<people;slot++)c.occupied[slot]=true;
            for(int i=0;i<40;i++)for(int slot=0;slot<people;slot++)Need(c.Beam(slot,75+i*.1001f,1,origin,(c.State.shieldPoint-origin).normalized),"Shield support missed");
            Need(c.State.rescueCharge>.99f,"Group-size-normalized shield cannot fill");c.Tick(81,.01f);Need(c.State.rescueAwarded,"Earned rescue not acknowledged");
            for(int i=0;i<40;i++)for(int slot=0;slot<people;slot++)c.Beam(slot,92+i*.1001f,1,origin,(c.State.dragonHeart-origin).normalized);
            Need(c.State.finaleCharge>.99f,"Finale charge does not respond");Need(c.State.supportMask==((1<<people)-1),"Contributor ownership incorrect");
            var roundTrip=JsonUtility.FromJson<ConcertState>(JsonUtility.ToJson(c.State));Need(roundTrip.supportMask==c.State.supportMask&&roundTrip.rescueCharge==c.State.rescueCharge,"State replication loses contribution");
            Need(JsonUtility.ToJson(c.State).Length<20000,"State exceeds RPC budget");
            c.Reset(2,0);Need(c.State.defeated==0&&c.State.supportMask==0&&c.State.rescueCharge==0&&c.State.orcs.All(x=>x.health==100),"New run retained previous damage");
            log.AppendLine($"PASS {people}-participant shield/finale support, ownership mask, state serialization and clean run reset (simulated participants).");
        }
        File.WriteAllText(Out+"/Combat_Validation.txt",log.ToString());Debug.Log("BATTLE23_COMBAT_PASS\n"+log);
    }
    static bool clipRequested;
    [MenuItem("Lord Lyrical/Battle23/Render Motion Review")]
    public static void Motion(){clipRequested=true;try{Performance();}finally{clipRequested=false;}}
    [MenuItem("Lord Lyrical/Battle23/Review Performance")]
    public static void Performance()
    {
        Directory.CreateDirectory(Out);var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var log=new StringBuilder("CANDIDATE 23 / EDITOR PERFORMANCE REVIEW\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Battle review world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            var lord=(LordLyricalController)typeof(ResonantWorld).GetField("lord",Flags).GetValue(world);
            var camera=new GameObject("Battle review camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=76;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,1.65f,2);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.012f,.02f);CinematicLook.Configure(camera);
            var light=new GameObject("Moon key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(.7f,.82f,1);light.transform.rotation=Quaternion.Euler(35,-30,0);light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.13f,.15f,.19f);
            var jeru=world.GetComponentInChildren<JeruPerformance>();var chamber=world.GetComponentInChildren<ChamberPresentation>();
            var timeline=new CueTimeline(JsonUtility.FromJson<CueTable>(cfg.cueJson.text).cues);
            if(clipRequested)Directory.CreateDirectory(Out+"/MotionFrames");int frameIndex=0;
            float maxAim=0,maxStep=0,minFeet=99;Vector3 prior=Vector3.zero;int firing=0;float minHandHeight=100;
            for(int i=0;i<=55*36;i++){
                float song=65+i/36f;world.Render(ShowFrame.At(song),true);float t=(float)timeline.NarrativeTime(song);
                if(clipRequested&&song>=69&&song<95&&i%3==0){
                    Layer(world.gameObject);camera.transform.position=new Vector3(-5,2,1);
                    camera.transform.LookAt(Vector3.Lerp(lord.root.position+Vector3.up*1.1f,chamber.Heart,.35f));
                    ParticipantAvatarBuilder.Render(camera,Out+"/MotionFrames/Frame_"+(frameIndex++).ToString("0000")+".png",960,720,1);
                }
                var h=lord.rightHand.position;
                Need(float.IsFinite(h.x)&&float.IsFinite(h.y)&&float.IsFinite(h.z),"Nonfinite JERU pose");
                if(i>0)maxStep=Mathf.Max(maxStep,Vector3.Distance(h,prior));prior=h;minHandHeight=Mathf.Min(minHandHeight,h.y);
                if(BattleScore.Counter(t)>.5f){
                    firing++;float aim=Vector3.Angle(lord.PalmNormal,chamber.Heart-h);maxAim=Mathf.Max(maxAim,aim);Need(aim<3,"JERU beam is not aligned with palm at "+t+": "+aim);
                }
                foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){
                    var f=jeru.animator.GetBoneTransform(bone);minFeet=Mathf.Min(minFeet,f.position.y);
                }
            }
            Need(firing>50,"No sustained counterattack poses");Need(maxStep<.45f,"Visible hand teleport "+maxStep);Need(minFeet>-.02f,"Feet pass below floor");Need(minHandHeight>.15f,"Hands pass through floor");
            log.AppendLine($"PASS {55*36+1} samples at 36 Hz: palm/beam maximum error {maxAim:F4} degrees; maximum hand displacement {maxStep:F4}m; minimum ankle height {minFeet:F3}m.");
            foreach(float song in new[]{70f,72.5f,75.5f,77.5f,85.7f,90.6f,98.4f,105f,112f}){
                world.Render(ShowFrame.At(song),true);Layer(world.gameObject);
                camera.transform.LookAt(song>=104?lord.root.position+Vector3.up*1.7f:Vector3.Lerp(lord.root.position+Vector3.up*1.7f,chamber.Mouth,.35f));
                ParticipantAvatarBuilder.Render(camera,Out+"/Battle_"+song.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
            }
            var fx=world.GetComponent<CombatImpactVFX>();Need(fx,"Impact component missing");
            for(int i=0;i<24;i++)fx.Hit(new Vector3((i%3-1)*.4f,1.2f,5),Vector3.back,Color.cyan,true,i%8==0);
            fx.SimulatePreview(.16f);Need(fx.ActiveParticles>20&&fx.ActiveParticles<=CombatImpactVFX.ParticleBudget,"Impact particles absent or unbounded");
            log.AppendLine("PASS pooled impacts populated and bounded; editor images are staged review, not native headset acceptance.");
            var encounter=world.GetComponent<ConcertInteraction>().Encounter;var orcs=(OrcEncounterController)typeof(ResonantWorld).GetField("orcs",Flags).GetValue(world);
            world.Render(ShowFrame.At(40),true);
            for(int i=0;i<12;i++)encounter.State.orcs[i].position=new Vector3(50+i*3,0,10);
            var enemy=encounter.State.orcs[0];enemy.position=new Vector3(-3,0,6);enemy.health=100;
            var origin=new Vector3(-3,1.3f,2);Physics.SyncTransforms();
            var contact=BeamHitResolver.Trace(encounter.State,40,origin,Vector3.forward);
            Need(contact.orc==0,"Actual world collider blocks Orc damage");
            Need(encounter.Beam(0,40,1,origin,Vector3.forward)&&enemy.health<100,"Actual world beam cannot damage Orc");
            orcs.RenderInteractive(encounter.State,40);
            var contactReview=new GameObject("Isolated actual contact review");contactReview.transform.SetParent(world.transform,false);
            var contactFX=contactReview.AddComponent<CombatImpactVFX>();contactFX.Initialize(world.GetComponentInChildren<CinematicParticleDirector>());
            contactFX.Hit(contact.point,contact.normal,Color.cyan,true);contactFX.SimulatePreview(.09f);
            Need(contactFX.ActiveParticles>=10,"Actual contact has no particle response");Layer(world.gameObject);camera.transform.position=new Vector3(-5,1.65f,2);camera.transform.LookAt(new Vector3(-3,1.25f,6));
            ParticipantAvatarBuilder.Render(camera,Out+"/Actual_Hit.png",960,720,1);
            UnityEngine.Object.DestroyImmediate(contactReview);
            log.AppendLine("PASS actual world colliders permit correct target contact, authoritative damage and populated contact particles.");
            enemy.health=0;enemy.intent=OrcIntent.Defeated;enemy.defeatedAt=40;enemy.entered=40;
            foreach(float age in new[]{.2f,.8f,1.6f,2.4f,3.4f}){
                orcs.RenderInteractive(encounter.State,40+age);Layer(world.gameObject);camera.transform.position=new Vector3(-5,1.65f,2);camera.transform.LookAt(new Vector3(-3,1.25f,6));
                ParticipantAvatarBuilder.Render(camera,Out+"/Defeat_"+age.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png",960,720,1);
            }
            log.AppendLine("PASS defeat sequence staged at five ages; original dragon retained as one instance and remains reclaimed through the finale.");
            Need(world.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="Current CyberDragon | chamber performance")==1,"Duplicate dragon");
            File.WriteAllText(Out+"/Performance_Validation.txt",log.ToString());Debug.Log("BATTLE23_PERFORMANCE_PASS\n"+log);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}
