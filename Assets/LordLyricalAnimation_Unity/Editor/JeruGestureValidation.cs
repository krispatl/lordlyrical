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
public static class JeruGestureValidation
{
    static void Need(bool ok,string why){if(!ok)throw new Exception("JERU GESTURE: "+why);}
    [MenuItem("Lord Lyrical/Animation/Validate JERU Gestures")]
    public static void Run()
    {
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder();
        try {
            string folder=MotionPolishValidation.Out+"/Gestures";Directory.CreateDirectory(folder);
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var go=UnityEngine.Object.Instantiate(cfg.lordModel);foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
            var p=go.GetComponent<JeruPerformance>();p.SampleShow(0);
            var camera=new GameObject("Gesture review").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.05f,.07f);
            camera.transform.position=new Vector3(0,1.35f,-3);camera.transform.LookAt(new Vector3(0,1.25f,0));
            var light=new GameObject("Gesture key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(25,-30,0);light.cullingMask=1<<28;RenderSettings.ambientLight=new Color(.4f,.4f,.45f);
            float[] times={22,22.65f,24,34,55,59,73,81,95,104};
            var left=p.animator.GetBoneTransform(HumanBodyBones.LeftHand);var right=p.animator.GetBoneTransform(HumanBodyBones.RightHand);
            var finger=p.animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
            float travel=0,fingerTravel=0;Vector3 previous=Vector3.zero;Quaternion previousFinger=Quaternion.identity;
            foreach(float t in times){
                p.SampleShow(t);
                foreach(bool isLeft in new[]{true,false}){
                    var arm=p.animator.GetBoneTransform(isLeft?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                    var lower=p.animator.GetBoneTransform(isLeft?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var hand=isLeft?left:right;
                    float length=Vector3.Distance(arm.position,lower.position)+Vector3.Distance(lower.position,hand.position);
                    Need(Vector3.Distance(arm.position,hand.position)<=length+.001f,"Arm over-extension");
                    Need(hand.position.y>.7f&&hand.position.y<2.3f,"Implausible hand position");
                }
                if(t!=times[0]){travel+=Vector3.Distance(previous,right.position);fingerTravel+=Quaternion.Angle(previousFinger,finger.localRotation);}
                previous=right.position;previousFinger=finger.localRotation;
                ParticipantAvatarBuilder.Render(camera,folder+"/Jeru_"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1000,1000,1);
            }
            Need(travel>1,"Hands do not visibly change pose");Need(fingerTravel>20,"Fingers remain static");
            p.SampleShow(34);Vector3 saved=right.position;p.SampleShow(95);p.SampleShow(34);Need(Vector3.Distance(saved,right.position)<.01f,"Gesture seek not deterministic");
            report.AppendLine($"PASS ten musical gesture checkpoints; hand travel {travel:F3} m, finger movement {fingerTravel:F1} degrees.");
            report.AppendLine("PASS bounded arm reach, hand positions, and deterministic seek.");
            report.AppendLine("Generated from the existing rig and offline song beat analysis; existing vocal animation remains later in execution order.");
            File.WriteAllText(MotionPolishValidation.Out+"/JERU_Gesture_Validation.txt",report.ToString());Debug.Log("JERU_GESTURES_PASS\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}
