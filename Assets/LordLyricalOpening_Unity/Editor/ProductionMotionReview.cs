using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;

public static class ProductionMotionReview
{
    const string Frames="C:/Users/kris/Documents/Codex/2026-10-03/bui/work/production_polish/reel";
    [MenuItem("Lord Lyrical/Production/Render Motion Review")]
    public static void Render()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("PRODUCTION MOTION REVIEW / 24 FPS EDITOR SAMPLING\nHand measurements refer to JERU in every clip; wing and mouth measurements refer to the dragon. Orc joint motion and tracked avatars are validated separately.\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Review cast").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=78;
            camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.backgroundColor=new Color(.006f,.009f,.016f);camera.clearFlags=CameraClearFlags.SolidColor;CinematicLook.Configure(camera);
            var light=new GameObject("Moon key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.color=new Color(.63f,.77f,1);light.transform.rotation=Quaternion.Euler(35,-30,0);light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);
            var jeru=world.GetComponentInChildren<JeruPerformance>();var chamber=world.GetComponentInChildren<ChamberPresentation>();
            var hands=new[]{jeru.animator.GetBoneTransform(HumanBodyBones.LeftHand),jeru.animator.GetBoneTransform(HumanBodyBones.RightHand)};
            var dragon=world.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            var wings=dragon.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="wing_upper.L"||x.name=="wing_upper.R").ToArray();
            float[] starts={22,71,40.2f,74,94,105,58,81};int[] counts={96,96,96,96,144,96,288,96};string[] names={"JERU gestures","JERU walk","Orc combat","Dragon roar and flight","Crescendo","JERU final gestures","Single dragon reveal","Dragon evasive recovery"};
            for(int clip=0;clip<starts.Length;clip++){
                var dir=Frames+"/"+clip;Directory.CreateDirectory(dir);Vector3[] oldHands=null;Quaternion[] oldWings=null;Vector3 oldMouth=Vector3.zero;
                float handStep=0,wingStep=0,mouthStep=0;
                for(int f=0;f<counts[clip];f++){
                    float t=starts[clip]+f/24f;world.Render(ShowFrame.At(t),true);
                    camera.transform.position=clip==2?new Vector3(-4,1.65f,1):new Vector3(0,1.65f,2);
                    var target=clip==0||clip==1||clip==5?jeru.transform.position+Vector3.up*1.85f:clip==2?new Vector3(-2,1.8f,9):clip==3||clip==7?chamber.Mouth-Vector3.up:clip==6?new Vector3(0,7,11):Vector3.Lerp(new Vector3(0,2.8f,5),chamber.Mouth,.55f);
                    camera.transform.LookAt(target);
                    foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                    foreach(var caption in world.GetComponentsInChildren<TextMesh>(true))if(caption.name=="Spatial call fallback"||caption.name=="Concert interaction feedback"){
                        var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;caption.transform.position=camera.transform.position+forward*2.2f+Vector3.down*.9f;caption.transform.rotation=Quaternion.LookRotation(forward);
                    }
                    var nowHands=hands.Select(h=>h.position).ToArray();var nowWings=wings.Select(w=>w.localRotation).ToArray();
                    if(f>0){for(int i=0;i<2;i++)handStep=Mathf.Max(handStep,Vector3.Distance(nowHands[i],oldHands[i]));
                        if(clip==3||clip==4||clip==7){for(int i=0;i<nowWings.Length;i++)wingStep=Mathf.Max(wingStep,Quaternion.Angle(nowWings[i],oldWings[i]));mouthStep=Mathf.Max(mouthStep,Vector3.Distance(chamber.Mouth,oldMouth));}}
                    oldHands=nowHands;oldWings=nowWings;oldMouth=chamber.Mouth;
                    ParticipantAvatarBuilder.Render(camera,dir+"/"+f.ToString("D4")+".png",960,720,1);
                }
                report.AppendLine($"{names[clip]}: {counts[clip]} frames. Largest 1/24 s hand step {handStep:F3} m; wing step {wingStep:F2} degrees; mouth step {mouthStep:F3} m.");
                if(handStep>.35f||wingStep>20||mouthStep>.5f)report.AppendLine("REVIEW REQUIRED: pose derivative exceeds presentation threshold.");
            }
            report.AppendLine("Editor capture: LOD1, review cameras follow the performance. This does not measure headset frame rate or prove passthrough visibility.");
            File.WriteAllText(MotionPolishValidation.Out+"/Production/Motion_Review.txt",report.ToString());Debug.Log(report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
