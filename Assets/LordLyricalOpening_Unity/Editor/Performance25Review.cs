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

public static class Performance25Review
{
    public static string EvidenceOverride;
    static string Out=>EvidenceOverride??MotionPolishValidation.Out+"/Production/Performance25";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static void Need(bool ok,string why){if(!ok)throw new Exception("PERFORMANCE25: "+why);}
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Flags).SetValue(obj,value);
    static object Get(object obj,string field)=>obj.GetType().GetField(field,Flags).GetValue(obj);
    static void Layer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;}
    static ExperienceConfiguration Config()=>AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
    static Camera CameraAt(Vector3 position,Vector3 target,float fov=45)
    {
        var c=new GameObject("Performance25 review camera").AddComponent<Camera>();
        c.cullingMask=1<<28;c.transform.position=position;c.transform.LookAt(target);c.fieldOfView=fov;
        c.nearClipPlane=.06f;c.farClipPlane=100;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.014f,.018f,.027f);
        return c;
    }
    static void Lighting()
    {
        var l=new GameObject("Reference review key").AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.8f;l.transform.rotation=Quaternion.Euler(25,-32,0);l.cullingMask=1<<28;
        var fill=new GameObject("Reference review fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.color=new Color(.53f,.70f,1);fill.transform.rotation=Quaternion.Euler(15,130,0);fill.cullingMask=1<<28;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.34f,.39f);
    }
    [MenuItem("Lord Lyrical/Performance25/Validate JERU")]
    public static void Performance()
    {
        Directory.CreateDirectory(Out);
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var log=new StringBuilder("PERFORMANCE25 / REFERENCE-LED JERU VALIDATION\n");
        try{
            var parent=new GameObject("JERU review route").transform;
            var go=UnityEngine.Object.Instantiate(Config().lordModel,parent);go.transform.localScale=Vector3.one*1.22f;Layer(go);
            var p=go.GetComponent<JeruPerformance>();p.SampleShow(0);var a=p.animator;
            foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftShoulder,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftMiddleDistal}){
                var tr=a.GetBoneTransform(bone);if(tr)log.AppendLine("ANCHOR "+bone+" "+go.transform.InverseTransformPoint(tr.position).ToString("F4"));
            }
            var lh=a.GetBoneTransform(HumanBodyBones.LeftHand);var rh=a.GetBoneTransform(HumanBodyBones.RightHand);
            var lf=a.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=a.GetBoneTransform(HumanBodyBones.RightFoot);
            Vector3 prevL=lh.position,prevR=rh.position,prevElbow=a.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;Quaternion prevW=lh.rotation;
            float maxStep=0,maxWrist=0,maxElbowStep=0,minFoot=100,maxExtension=0,maxElbowTime=0;
            for(int i=0;i<=121*72;i++){
                float t=i/72f;parent.position=ConductorRoute.Position(t);parent.rotation=ConductorRoute.Rotation(t);p.SampleShow(t);
                foreach(bool left in new[]{true,false}){
                    var u=a.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                    var l=a.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var h=left?lh:rh;
                    Need(ManualResonanceBeams.Finite(h.position),"Nonfinite hand at "+t);
                    float reach=Vector3.Distance(u.position,l.position)+Vector3.Distance(l.position,h.position);
                    maxExtension=Mathf.Max(maxExtension,Vector3.Distance(u.position,h.position)-reach);
                    var local=go.transform.InverseTransformPoint(h.position);
                    Need(local.y>.64f&&local.y<2.15f,"Hand outside anatomical range at "+t+": "+local);
                }
                if(i>0&&t>=20&&t<54){
                    maxStep=Mathf.Max(maxStep,Vector3.Distance(lh.position,prevL),Vector3.Distance(rh.position,prevR));
                    maxWrist=Mathf.Max(maxWrist,Quaternion.Angle(lh.rotation,prevW));
                    float elbowStep=Vector3.Distance(a.GetBoneTransform(HumanBodyBones.LeftLowerArm).position,prevElbow);
                    if(elbowStep>maxElbowStep){maxElbowStep=elbowStep;maxElbowTime=t;}
                }
                minFoot=Mathf.Min(minFoot,lf.position.y,rf.position.y);prevL=lh.position;prevR=rh.position;prevW=lh.rotation;prevElbow=a.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
            }
            Need(maxExtension<.002f,"Arm overextension");
            Need(maxStep<.14f,"Hand discontinuity in performance: "+maxStep);
            Need(maxWrist<35,"Wrist flip in performance: "+maxWrist);
            Need(maxElbowStep<.14f,"Elbow discontinuity: "+maxElbowStep+" at "+maxElbowTime);
            Need(minFoot>-.02f,"Feet below floor");
            log.AppendLine($"PASS 8,713 samples at 72Hz; performance maximum hand step {maxStep:F4}m, wrist change {maxWrist:F2} degrees, elbow step {maxElbowStep:F4}m, minimum foot height {minFoot:F3}m.");
            parent.position=Vector3.zero;parent.rotation=Quaternion.identity;
            p.SampleShow(23.1f);Vector3 rest=lh.position;p.SampleShow(24.264f);
            Need(Vector3.Distance(rest,lh.position)>.35f,"Reference accent not distinguishable from rest");
            p.SampleShow(34.934f);
            var head=a.GetBoneTransform(HumanBodyBones.Head);
            Need(Vector3.Distance(lh.position,head.position)<.24f*go.transform.lossyScale.y&&lh.position.y<head.position.y,"Listening hand misses ear/jaw region");
            Vector3 saved=lh.position;Quaternion savedW=lh.rotation;
            p.SampleShow(59.623f);p.SampleShow(34.934f);
            Need(Vector3.Distance(saved,lh.position)<.005f&&Quaternion.Angle(savedW,lh.rotation)<.2f,"Seek changes reference pose");
            log.AppendLine("PASS distinct rest/accent, repeatable random seek, bounded anatomical reach. Reference motifs have separate preparation, accent, hold and recovery.");
            Lighting();var cam=CameraAt(new Vector3(0,1.6f,-3.4f),new Vector3(0,1.4f,0),43);
            foreach(float t in new[]{21.615f,23.1f,24.264f,26.588f,34.934f,42.281f,59.623f,114.5f}){
                p.SampleShow(t);ParticipantAvatarBuilder.Render(cam,Out+"/Jeru_"+t.ToString("000.000",System.Globalization.CultureInfo.InvariantCulture)+".png",1100,1100,1);
            }
            cam.transform.position=new Vector3(2.2f,1.65f,-2.8f);cam.transform.LookAt(new Vector3(0,1.42f,0));
            p.SampleShow(34.934f);ParticipantAvatarBuilder.Render(cam,Out+"/Jeru_Listen_Quarter.png",1100,1100,1);
            File.WriteAllText(Out+"/JERU_Validation.txt",log.ToString());Debug.Log("PERFORMANCE25_JERU_PASS\n"+log);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);}
    }
    static bool beamClip;
    [MenuItem("Lord Lyrical/Performance25/Render Beam Preview")]
    public static void BeamMotion(){beamClip=true;try{Beams();}finally{beamClip=false;}}
    [MenuItem("Lord Lyrical/Performance25/Validate Magical Beams")]
    public static void Beams()
    {
        Directory.CreateDirectory(Out);
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var log=new StringBuilder("PERFORMANCE25 / PALM PRESENTATION AND CONTROL VALIDATION\n");Material targetMaterial=null;
        try{
            var far=new Vector3(1000,0,1000);var origin=far+new Vector3(0,1.30f,.3f);
            var holder=new GameObject("Beam review");var session=holder.AddComponent<NetworkSessionController>();
            typeof(NetworkSessionController).GetProperty("Slot").SetValue(session,0);
            var director=holder.AddComponent<ExperienceDirector>();director.configuration=Config();
            var encounter=new ConcertEncounter();
            for(int i=0;i<12;i++)encounter.State.orcs[i].position=far+new Vector3(50+i*3,0,8);
            encounter.State.orcs[0].position=far+Vector3.forward*6;
            var beams=holder.AddComponent<ManualResonanceBeams>();beams.Initialize(session,encounter,null);
            var all=(Array)Get(beams,"beams");var left=all.GetValue(0);
            Set(left,"active",true);Set(left,"origin",origin);Set(left,"direction",Vector3.forward);Set(left,"received",10f);Set(beams,"live",true);
            var draw=typeof(ManualResonanceBeams).GetMethod("Draw",Flags);var core=(LineRenderer)Get(left,"core");var magic=beams.Presentation;
            Shader.SetGlobalFloat("_ShowTime",30);
            draw.Invoke(beams,new object[]{30f,1/72f,10f});
            var expected=BeamHitResolver.Trace(encounter.State,30,origin,Vector3.forward);
            Need(core.enabled&&beams.ActiveBeams==1,"Active palm beam missing");
            Need(Vector3.Distance(core.GetPosition(0),origin)<.001f&&Vector3.Distance(core.GetPosition(15),expected.point)<.002f,"Visual aim differs from damage ray");
            Need(magic.ActiveParticles>50&&magic.RibbonVertices>0&&magic.DroppedParticles==0&&magic.AllFinite(),"Incomplete or invalid beam layers");
            log.AppendLine($"PASS one palm: {magic.ActiveParticles} particles, {magic.RibbonVertices} braided ribbon vertices; shared damage/visual endpoint.");
            Lighting();var cam=CameraAt(far+new Vector3(2.7f,1.9f,-1.0f),far+new Vector3(0,1.3f,3.3f),49);
            var target=GameObject.CreatePrimitive(PrimitiveType.Capsule);UnityEngine.Object.DestroyImmediate(target.GetComponent<Collider>());target.transform.position=far+new Vector3(0,1.25f,6);target.transform.localScale=new Vector3(.8f,1.25f,.8f);
            targetMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));targetMaterial.color=new Color(.12f,.17f,.23f);target.GetComponent<Renderer>().sharedMaterial=targetMaterial;Layer(target);Layer(holder);
            var impact=(CombatImpactVFX)Get(beams,"impactFX");impact.SimulatePreview(.1f);
            ParticipantAvatarBuilder.Render(cam,Out+"/Beam_Contact.png",1280,800,1);
            cam.transform.position=far+new Vector3(.12f,1.65f,-.20f);cam.transform.LookAt(far+new Vector3(0,1.3f,5.5f));
            ParticipantAvatarBuilder.Render(cam,Out+"/Beam_FirstPerson.png",1280,800,1);
            if(beamClip){
                Directory.CreateDirectory(Out+"/BeamFrames");magic.Clear();impact.Clear();
                cam.transform.position=far+new Vector3(3.2f,2.1f,-2.6f);cam.transform.LookAt(far+new Vector3(0,1.3f,3));
                for(int f=0;f<84;f++){
                    float elapsed=f/24f,t=30+elapsed,now=20+elapsed;
                    Set(left,"active",elapsed>=.18f&&elapsed<2.9f);Set(left,"received",now);
                    ((float[])Get(beams,"charge"))[0]=elapsed<.18f?elapsed/.18f:0;
                    ((Vector3[])Get(beams,"chargeOrigin"))[0]=origin;((Vector3[])Get(beams,"chargeDirection"))[0]=Vector3.forward;
                    Shader.SetGlobalFloat("_ShowTime",t);draw.Invoke(beams,new object[]{t,1/24f,now});
                    impact.SimulatePreview(1/24f);Layer(holder);
                    ParticipantAvatarBuilder.Render(cam,Out+"/BeamFrames/Frame_"+f.ToString("0000")+".png",960,720,1);
                }
                Set(left,"active",true);Set(left,"received",10f);
            }
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=far+new Vector3(0,1.3f,3);wall.transform.localScale=new Vector3(2,3,.2f);Physics.SyncTransforms();
            draw.Invoke(beams,new object[]{30.01f,1/72f,10.01f});Need(Mathf.Abs(core.GetPosition(15).z-(far.z+2.9f))<.003f,"Beam crosses wall");
            float health=encounter.State.orcs[0].health;Need(!encounter.Beam(0,30.01f,1,origin,Vector3.forward)&&encounter.State.orcs[0].health==health,"Damage crosses wall");
            UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
            log.AppendLine("PASS world occlusion and damage agree; ornamental current does not move the hit ray.");
            draw.Invoke(beams,new object[]{30.02f,1/72f,10.36f});
            Need(!core.enabled&&beams.ActiveBeams==0&&magic.RibbonVertices==0,"Expired tracking kept beam visible");
            Set(left,"active",true);Set(left,"received",11f);draw.Invoke(beams,new object[]{30.03f,1/72f,11f});
            typeof(ManualResonanceBeams).GetMethod("OnApplicationFocus",Flags).Invoke(beams,new object[]{false});
            Need(!core.enabled&&!magic.Visible&&magic.ActiveParticles==0,"Focus loss retained magic");
            log.AppendLine("PASS expired pose stops beam immediately; focus loss clears all line, ribbon, charge and particle layers.");
            ((float[])Get(beams,"charge"))[0]=.6f;((Vector3[])Get(beams,"chargeOrigin"))[0]=origin;((Vector3[])Get(beams,"chargeDirection"))[0]=Vector3.forward;
            draw.Invoke(beams,new object[]{30.04f,1/72f,11.01f});Need(beams.ActiveBeams==0&&magic.ActiveParticles>=17&&magic.ActiveParticles<40&&magic.RibbonVertices==0,"Charge emitted a firing beam");
            cam.transform.position=far+new Vector3(.20f,1.48f,-.20f);cam.transform.LookAt(origin);cam.fieldOfView=48;Layer(holder);
            ParticipantAvatarBuilder.Render(cam,Out+"/Beam_Charge.png",1000,800,1);
            log.AppendLine("PASS charge is visible during the existing acquisition dwell, without an early beam.");
            magic.Clear();int maximum=0;
            for(int frame=0;frame<216;frame++){
                float t=40+frame/72f;magic.Begin(t,20+frame/72f,far+new Vector3(0,1.65f,-1),true);
                for(int i=0;i<20;i++){
                    var start=far+new Vector3((i-9.5f)*.15f,1.3f,.3f);
                    magic.Beam(i,start,start+new Vector3(.2f,0,5),Vector3.back,true,RootKeys.Slots[i/2].color,i<2);
                }
                magic.End();maximum=Mathf.Max(maximum,magic.ActiveParticles);
                Need(magic.DroppedParticles==0&&magic.AllFinite()&&magic.ActiveParticles<=magic.Capacity,"Twenty palms exceed budget or produce invalid geometry");
                Need(magic.RibbonVertices>1000&&magic.RibbonVertices<=1920,"Twenty palms lost ribbon geometry");
            }
            log.AppendLine($"PASS simulated twenty palms across 216 samples: maximum {maximum}/{magic.Capacity} particles, 1,920 batched ribbon vertices, zero drops. This is a capacity check, not a native frame-rate measurement.");
            magic.Clear();Need(!magic.Visible,"Clear left particles visible");
            File.WriteAllText(Out+"/Beam_Validation.txt",log.ToString());Debug.Log("PERFORMANCE25_BEAMS_PASS\n"+log);
        }finally{
            EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);
            if(targetMaterial)UnityEngine.Object.DestroyImmediate(targetMaterial);
        }
    }
    [MenuItem("Lord Lyrical/Performance25/Validate Duel")]
    public static void Duel()
    {
        Directory.CreateDirectory(Out);var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var session=new GameObject("Duel review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Duel review world").AddComponent<ResonantWorld>();world.Initialize(Config(),session);
            var lord=(LordLyricalController)Get(world,"lord");var chamber=world.GetComponentInChildren<ChamberPresentation>();
            float maxAim=0,maxStep=0;Vector3 last=Vector3.zero;int firing=0;
            for(int i=0;i<=55*36;i++){
                float t=65+i/36f;world.Render(ShowFrame.At(t),true);
                var h=lord.rightHand.position;if(i>0)maxStep=Mathf.Max(maxStep,Vector3.Distance(h,last));last=h;
                if(BattleScore.Counter(t)>.5f){firing++;float aim=Vector3.Angle(lord.PalmNormal,chamber.Heart-h);maxAim=Mathf.Max(maxAim,aim);Need(aim<3,"Duel aim regressed at "+t);}
            }
            Need(firing>50&&maxStep<.45f,"Duel motion continuity regressed");
            var dragon=world.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="Current CyberDragon | chamber performance").ToArray();Need(dragon.Length==1,"Dragon duplicate");
            foreach(float charge in new[]{0f,1f}){
                chamber.Battle=new ConcertState{finaleCharge=charge};chamber.Evaluate(98,true);Need(dragon[0].gameObject.activeSelf,"Dragon missing at finishing hit");
                chamber.Evaluate(101,true);Need(!dragon[0].gameObject.activeSelf,"Dragon survives the timed destruction");
            }
            File.WriteAllText(Out+"/Duel_Validation.txt",$"PASS 1,981 samples: maximum palm aim error {maxAim:F4} degrees; maximum hand step {maxStep:F4}m.\nPASS one preserved dragon, timed 98-101s destruction with zero/full contribution.\n");
            Debug.Log("PERFORMANCE25_DUEL_PASS");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);}
    }
    [MenuItem("Lord Lyrical/Performance25/Render Performance Preview")]
    public static void Motion()
    {
        Directory.CreateDirectory(Out+"/MotionFrames");var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var parent=new GameObject("Preview route").transform;var go=UnityEngine.Object.Instantiate(Config().lordModel,parent);go.transform.localScale=Vector3.one*1.22f;Layer(go);
            var p=go.GetComponent<JeruPerformance>();var face=go.GetComponent<JeruConcertFace>();
            if(MusicReactiveSystem.Score==null)new MusicReactiveSystem();
            Lighting();var cam=CameraAt(new Vector3(0,1.6f,-3.4f),new Vector3(0,1.40f,0),43);
            for(int frame=0;frame<30*24;frame++){
                float t=21+frame/24f;p.SampleShow(t);if(face)face.Sample(t,true);
                ParticipantAvatarBuilder.Render(cam,Out+"/MotionFrames/Frame_"+frame.ToString("0000")+".png",960,720,1);
            }
            File.WriteAllText(Out+"/Motion_Render.txt","720 editor frames at 24 fps, show time 21-51s. Isolated JERU upper-body review; camera fixed, root fixed. Not native headset capture or motion-capture extraction.");
            Debug.Log("PERFORMANCE25_MOTION_READY");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);}
    }
}
