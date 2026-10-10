using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;
public static class MotionPolishValidation
{
    public const string Out="C:/Users/kris/Documents/Codex/2026-10-03/bui/outputs/LordLyrical_Animation_Polish";
    static void Need(bool pass,string why){if(!pass)throw new Exception("MOTION VALIDATION: "+why);}
    static Quaternion[] Pose(Transform[] nodes)=>nodes.Select(t=>t.localRotation).ToArray();
    static float Delta(Quaternion[] a,Quaternion[] b)=>a.Select((q,i)=>Quaternion.Angle(q,b[i])).Max();
    static void Layer(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;}
    [MenuItem("Lord Lyrical/Animation/Validate Final Motion")]
    public static void Run()
    {
        Directory.CreateDirectory(Out+"/Poses");
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("FINAL POSE VALIDATION\n");
        try {
            int worldOrder=((DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(ExperienceDirector),typeof(DefaultExecutionOrder))).order;
            int speechOrder=((DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(CrazyMinnow.SALSA.QueueProcessor),typeof(DefaultExecutionOrder))).order;
            int faceOrder=((DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(JeruConcertFace),typeof(DefaultExecutionOrder))).order;
            Need(worldOrder<speechOrder&&speechOrder<faceOrder,"Body/speech/expression execution order");
            report.AppendLine($"PASS final pose order: world {worldOrder}, speech {speechOrder}, expressions {faceOrder}");
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var camera=new GameObject("Motion review").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=52;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.05f);
            var light=new GameObject("Motion key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,-25,0);light.cullingMask=1<<28;
            RenderSettings.ambientLight=new Color(.35f,.4f,.48f);
            var orc=UnityEngine.Object.Instantiate(cfg.orcModel);Layer(orc);var perf=orc.GetComponent<SonicOrcPerformance>();
            var a=perf.animator;var bones=a.GetComponentsInChildren<Transform>().Where(t=>new[]{"calf_l","calf_r","upperarm_l","upperarm_r","hand_r"}.Contains(t.name)).ToArray();
            if(cfg.orcWeapon){var weapon=UnityEngine.Object.Instantiate(cfg.orcWeapon,perf.rightWeaponSocket);weapon.transform.localPosition=Vector3.zero;weapon.transform.localRotation=Quaternion.identity;Layer(weapon);}
            camera.transform.position=new Vector3(3,2.6f,5.8f);camera.transform.LookAt(new Vector3(0,1.45f,0));
            for(int clip=0;clip<11;clip++){
                perf.SampleClip(clip,.08f);var first=Pose(bones);perf.SampleClip(clip,.48f);var second=Pose(bones);
                Need(a.runtimeAnimatorController==null,"Orc still has a competing controller");
                Need(Delta(first,second)>2,"Orc motion is static: "+clip);a.Update(.016f);
                float overwrite=Delta(second,Pose(bones));if(clip!=2&&clip!=4)Need(overwrite<.1f,"Orc base pose overwritten: "+clip);
                // The world performs physical corrections in LateUpdate, after automatic animation.
                perf.SampleClip(clip,.48f);
                report.AppendLine($"PASS Orc clip {clip}: motion {Delta(first,second):F2} degrees, post-update overwrite {overwrite:F3}");
                for(int frame=0;frame<4;frame++){
                    float length=clip==4?2.4f:clip==5?.833333f:clip==0?1.5f:clip==1||clip==7||clip==9?1:.666667f;
                    a.Update(0);perf.SampleClip(clip,(frame+.3f)/4*length);
                    ParticipantAvatarBuilder.Render(camera,Out+"/Poses/Orc_"+clip.ToString("00")+"_"+frame+".png",960,960,1);
                }
            }
            orc.SetActive(false);
            var jeru=UnityEngine.Object.Instantiate(cfg.lordModel);Layer(jeru);var jp=jeru.GetComponent<JeruPerformance>();
            var jb=new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm}.Select(jp.animator.GetBoneTransform).ToArray();
            jp.SampleShow(73);var j1=Pose(jb);jp.SampleShow(73.3f);var j2=Pose(jb);jp.animator.Update(.016f);
            Need(Delta(j1,j2)>5,"JERU motion static");Need(Quaternion.Angle(j2[0],jb[0].localRotation)<.1f&&Quaternion.Angle(j2[1],jb[1].localRotation)<.1f,"JERU base walking pose overwritten");jp.SampleShow(73.3f);Need(Delta(j2,Pose(jb))<.1f,"JERU final gesture reconstruction changed");Need(jp.animator.runtimeAnimatorController==null,"JERU competing controller");
            report.AppendLine($"PASS JERU walk + conducting: {Delta(j1,j2):F2} degrees, no competing preview controller");
            for(int i=0;i<4;i++){jp.SampleShow(73+i*.17f);ParticipantAvatarBuilder.Render(camera,Out+"/Poses/Jeru_"+i+".png",960,960,1);}
            jeru.SetActive(false);
            for(int slot=0;slot<10;slot++){
                var avatar=UnityEngine.Object.Instantiate(cfg.participantModels[slot]);var ap=avatar.GetComponent<ParticipantAvatarPerformance>();
                ap.SampleTracked(0);Need(ap.animator.runtimeAnimatorController==null,"Avatar controller conflict "+slot);
                var type=typeof(ParticipantAvatarPerformance);type.GetField("smoothedVelocity",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(ap,Vector3.forward*1.8f);
                type.GetField("distanceTravelled",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(ap,.1f);ap.SampleTracked(.1f);
                var legs=new[]{ap.animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),ap.animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)};var v1=Pose(legs);
                type.GetField("distanceTravelled",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(ap,.8f);ap.SampleTracked(.5f);var v2=Pose(legs);ap.animator.Update(.016f);
                Need(Delta(v1,v2)>5,"Avatar feet static "+slot);Need(Delta(v2,Pose(legs))<.1f,"Avatar pose overwritten "+slot);
                report.AppendLine($"PASS participant {slot+1}: tracked locomotion {Delta(v1,v2):F2} degrees, no overwrite");
                UnityEngine.Object.DestroyImmediate(avatar);
            }
            var chamber=cfg.chamberModel.GetComponentInChildren<ChamberPresentation>(true);
            var dragon=UnityEngine.Object.Instantiate(cfg.dragonModel);Layer(dragon);var da=dragon.GetComponent<Animator>();da.runtimeAnimatorController=null;da.enabled=false;
            var dp=new DragonPerformance(dragon.transform,chamber.unfoldClip,chamber.flyClip,chamber.roarClip);
            var dn=dragon.GetComponentsInChildren<Transform>().Where(t=>t.name=="wing_upper.L"||t.name=="wing_upper.R"||t.name=="tail_08"||t.name=="neck_03").ToArray();
            camera.fieldOfView=58;camera.transform.position=new Vector3(13,8,-18);camera.transform.LookAt(new Vector3(0,3,0));
            dp.SampleShow(70,Vector3.zero,1);var d1=Pose(dn);dp.SampleShow(71,Vector3.zero,1);var d2=Pose(dn);
            Need(Delta(d1,d2)>12,"Dragon too static");dp.SampleShow(70,Vector3.zero,1);Need(Delta(d1,Pose(dn))<.1f,"Dragon seek is nondeterministic");
            report.AppendLine($"PASS dragon wing/neck/tail motion {Delta(d1,d2):F2} degrees; deterministic seek");
            float[] times={63,65,67,69,70,71,74.5f,76,79,85,90.3f,92,96,98.5f};
            foreach(float t in times){
                dp.SampleShow(t,Vector3.zero,1);
                Need(dragon.transform.localScale==Vector3.one,"Dragon scale distorted");
                foreach(var tr in dragon.GetComponentsInChildren<Transform>())Need(float.IsFinite(tr.position.x)&&float.IsFinite(tr.position.y)&&float.IsFinite(tr.position.z),"Non-finite dragon pose");
                ParticipantAvatarBuilder.Render(camera,Out+"/Poses/Dragon_"+t.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,900,1);
            }
            File.WriteAllText(Out+"/Motion_Validation.txt",report.ToString());Debug.Log("FINAL_MOTION_VALIDATION_PASS\n"+report);
        } finally {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}