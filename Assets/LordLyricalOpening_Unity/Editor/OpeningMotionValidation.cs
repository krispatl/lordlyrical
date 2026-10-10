using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;
public static class OpeningMotionValidation
{
    const string Out=MotionPolishValidation.Out;
    static void Need(bool ok,string why){if(!ok)throw new Exception("OPENING VALIDATION: "+why);}
    static void Layer(GameObject go){foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;}
    static Vector3 Bend(Animator a,bool left)=>AnatomicalIK.PreserveBend(a.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg),a.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg),a.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot),Vector3.zero);
    [MenuItem("Lord Lyrical/Opening/Validate Opening and Anatomy")]
    public static void Run()
    {
        Directory.CreateDirectory(Out+"/Opening");
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("OPENING AND ANATOMY VALIDATION\n");
        PassthroughOpening opening=null;
        try {
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var camera=new GameObject("Opening review camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=72;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.065f,.08f);
            var light=new GameObject("Opening key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,-25,0);light.cullingMask=1<<28;
            RenderSettings.ambientLight=new Color(.35f,.4f,.48f);
            for(int i=0;i<4;i++){
                var origin=new GameObject("Origin math").transform;var head=new GameObject("Head math").transform;head.SetParent(origin,false);
                origin.SetPositionAndRotation(new Vector3(i*2,0,-i),Quaternion.Euler(0,i*51,0));head.localPosition=new Vector3(i*.2f,1.7f,-.3f*i);head.localRotation=Quaternion.Euler(12,i*39,0);
                SharedRoomAlignment.AlignOrigin(origin,head);
                Need(Vector3.Distance(Vector3.ProjectOnPlane(head.position,Vector3.up),SharedRoomAlignment.VenueMark)<.001f,"Shared mark mismatch");
                Need(Vector3.Dot(Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized,Vector3.forward)>.9999f,"Shared heading mismatch");
                Need(Mathf.Abs(head.position.y-1.7f)<.001f,"Alignment changed eye height");
                UnityEngine.Object.DestroyImmediate(origin.gameObject);
            }
            report.AppendLine("PASS four independent tracking origins align to the same floor mark and stage heading; eye height preserved.");
            var raw=UnityEngine.Object.Instantiate(cfg.lordModel);var corrected=UnityEngine.Object.Instantiate(cfg.lordModel);Layer(corrected);raw.SetActive(true);
            var rp=raw.GetComponent<JeruPerformance>();var cp=corrected.GetComponent<JeruPerformance>();rp.SampleShow(0);cp.SampleShow(0);
            var field=typeof(JeruPerformance).GetField("feet",BindingFlags.NonPublic|BindingFlags.Instance);field.SetValue(rp,null);
            float minBendDot=1,minFacing=1;
            for(int f=0;f<180;f++){
                float t=70.5f+f*.09f;
                raw.transform.SetPositionAndRotation(ConductorRoute.Position(t),ConductorRoute.Rotation(t));
                corrected.transform.SetPositionAndRotation(ConductorRoute.Position(t),ConductorRoute.Rotation(t));
                rp.SampleShow(t);cp.SampleShow(t);
                foreach(bool left in new[]{true,false}){
                    var a=Bend(rp.animator,left);var b=Bend(cp.animator,left);
                    float dot=Vector3.Dot(a,b);minBendDot=Mathf.Min(minBendDot,dot);Need(dot>.65f,"Grounding reversed a knee at "+t);
                }
                var facing=corrected.transform.TransformDirection(HumanoidLocomotion.Forward(cp.animator,corrected.transform));
                float fd=Vector3.Dot(facing.normalized,ConductorRoute.Velocity(t).normalized);minFacing=Mathf.Min(minFacing,fd);Need(fd>.75f,"JERU faces away from walking route at "+t);
                if(f%45==0){
                    camera.transform.position=corrected.transform.position+corrected.transform.rotation*new Vector3(2,1.3f,-3);
                    camera.transform.LookAt(corrected.transform.position+Vector3.up*1.2f);
                    ParticipantAvatarBuilder.Render(camera,Out+"/Opening/Jeru_Walk_"+f+".png",1000,1000,1);
                }
            }
            report.AppendLine($"PASS 180 grounded JERU route poses: minimum knee preservation dot {minBendDot:F3}, minimum facing/travel dot {minFacing:F3}.");
            UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(corrected);
            for(int slot=0;slot<10;slot++){
                var avatar=UnityEngine.Object.Instantiate(cfg.participantModels[slot]);var ap=avatar.GetComponent<ParticipantAvatarPerformance>();ap.SampleTracked(0);
                var head=new GameObject("Tracked head test").transform;head.position=new Vector3(0,1.72f,0);head.rotation=Quaternion.Euler(0,37,0);
                ap.ApplyTrackedBody(head,null,null,0);
                var forward=avatar.transform.TransformDirection(HumanoidLocomotion.Forward(ap.animator,avatar.transform));
                Need(Vector3.Dot(forward.normalized,head.forward)>.97f,"Participant body reversed "+slot);
                ap.SampleTracked(.1f);head.position=new Vector3(0,1.35f,0);ap.ApplyTrackedBody(head,null,null,0);
                foreach(bool left in new[]{true,false}){
                    var knee=Bend(ap.animator,left);Need(Vector3.Dot(knee,head.forward)>.3f,"Participant crouch knee reversed "+slot);
                }
                Need(!ap.IsNarrativelyHidden,"Tracked participant hidden by authored choreography");
                UnityEngine.Object.DestroyImmediate(head.gameObject);UnityEngine.Object.DestroyImmediate(avatar);
            }
            report.AppendLine("PASS all ten tracked avatar bodies face the wearer, preserve forward crouch knees, and stay tracking-owned.");
            var session=new GameObject("Preview network only").AddComponent<NetworkSessionController>();session.configuration=cfg;
            var world=new GameObject("Preview world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            opening=new PassthroughOpening(world,cfg,camera);
            Layer(world.gameObject);Layer(opening.VisualRoot.gameObject);
            camera.transform.position=new Vector3(0,1.65f,2);camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,1.6f,5)-camera.transform.position);
            var founder=world.transform.Find("Lord Lyrical - eleventh presence");Need(founder!=null,"JERU missing");
            var founderRenderers=founder.GetComponentsInChildren<Renderer>(true);
            float[] times={-17,-2.01f,-1.7f,-.7f,.5f,3,6.5f,8.5f,9};
            foreach(float t in times){
                world.Render(ShowFrame.At(Mathf.Max(0,t)),t>=0);opening.Render(t);
                Need(t>=9||(!camera.allowHDR&&!camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing),"Passthrough alpha lost to HDR or post processing");
                Need(Mathf.Abs(opening.RoomOpacity-OpeningTiming.RoomOpacity(t))<.001f,"Room fade incorrect");
                Need(t>=-2||founderRenderers.All(r=>r.forceRenderingOff),"JERU visible before arrival");
                if(t>=-2&&t<0)Need(founderRenderers.Any(r=>!r.forceRenderingOff),"JERU hidden during arrival");
                if(t<8)Need(world.GetComponentsInChildren<Renderer>(true).Where(r=>!founderRenderers.Contains(r)).All(r=>r.forceRenderingOff),"Virtual world leaks into passthrough");
                if(t>=9)Need(world.GetComponentsInChildren<Renderer>(true).All(r=>!r.forceRenderingOff),"World remains hidden in VR");
                if(t>=0&&t<9)Need(OpeningTiming.PortalCenter.z>founder.position.z,"Portal is not behind JERU");
                Need(float.IsFinite(founder.position.y),"Invalid arrival transform");
                ParticipantAvatarBuilder.Render(camera,Out+"/Opening/Arrival_"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
                report.AppendLine($"PASS song time {t:F2}: {opening.Stage}, room {opening.RoomOpacity:F2}, JERU height {founder.position.y:F2}, particles {opening.ParticleCount}");
            }
            Need(Vector3.Distance(SharedRoomAlignment.VenueMark,OpeningTiming.JeruPosition(0))==3,"Viewing mark not three metres");
            Need(cfg.countdownDuration==17,"Prelude duration changed");
            report.AppendLine("PASS three-metre central viewing mark and 17-second music prelude.");
            report.AppendLine("Editor previews show virtual layers only; native camera passthrough requires PICO validation.");
            File.WriteAllText(Out+"/Opening_Anatomy_Validation.txt",report.ToString());Debug.Log("OPENING_ANATOMY_VALIDATION_PASS\n"+report);
        } catch(Exception e){File.WriteAllText(Out+"/Opening_Anatomy_Validation.txt",report+"\nFAIL "+e);throw;}
        finally {opening?.Dispose();EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}
