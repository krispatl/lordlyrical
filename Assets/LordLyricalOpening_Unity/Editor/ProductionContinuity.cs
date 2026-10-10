using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;

public static class ProductionContinuity
{
    [MenuItem("Lord Lyrical/Production/Audit Full Show Motion")]
    public static void Run()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Audit session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Audit world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);world.Render(ShowFrame.At(0),true);
            var jeru=world.GetComponentInChildren<JeruPerformance>();
            var tracks=new List<(string name,Transform bone,float warning)>();
            foreach(var id in new[]{HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot})
                tracks.Add(("JERU "+id,jeru.animator.GetBoneTransform(id),.16f));
            int orcIndex=0;foreach(var orc in world.GetComponentsInChildren<SonicOrcPerformance>(true)){
                tracks.Add(("Orc "+orcIndex+" grip",orc.rightWeaponSocket,.35f));orcIndex++;
            }
            var dragon=world.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            foreach(var name in new[]{"head","wing_upper.L","wing_upper.R","tail_13"})
                tracks.Add(("Dragon "+name,dragon.GetComponentsInChildren<Transform>(true).Single(x=>x.name==name),.45f));
            var old=new Vector3[tracks.Count];var oldRotation=new Quaternion[tracks.Count];var active=new bool[tracks.Count];
            var max=new float[tracks.Count];var worstTime=new float[tracks.Count];var maxAngle=new float[tracks.Count];var worstAngleTime=new float[tracks.Count];
            int invalid=0;
            for(int frame=0;frame<=121*72;frame++){
                float time=frame/72f;world.Render(ShowFrame.At(time),true);
                for(int i=0;i<tracks.Count;i++){
                    var bone=tracks[i].bone;if(!bone)continue;bool visible=bone.gameObject.activeInHierarchy;var point=bone.position;
                    if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z))invalid++;
                    if(frame>0&&active[i]&&visible){
                        float step=Vector3.Distance(point,old[i]);
                        if(step>max[i]){max[i]=step;worstTime[i]=time;}
                        float angle=Quaternion.Angle(oldRotation[i],bone.rotation);if(angle>maxAngle[i]){maxAngle[i]=angle;worstAngleTime[i]=time;}
                    }
                    old[i]=point;oldRotation[i]=bone.rotation;active[i]=visible;
                }
            }
            var report=new StringBuilder("FULL SHOW MOTION CONTINUITY AUDIT\n8713 samples at 72 Hz; authored timeline in the editor. This is not native headset frame-rate testing.\n");
            int flags=0;for(int i=0;i<tracks.Count;i++){
                bool flag=max[i]>tracks[i].warning||maxAngle[i]>(tracks[i].name.StartsWith("Orc")?20:12);if(flag)flags++;
                report.AppendLine($"{(flag?"REVIEW":"OK")} {tracks[i].name}: largest one-frame translation {max[i]:F4} m at {worstTime[i]:F4}s, largest rotation {maxAngle[i]:F2} degrees at {worstAngleTime[i]:F4}s.");
            }
            report.AppendLine($"Nonfinite positions: {invalid}; translation/rotation review flags: {flags}. Hidden-to-visible entrances excluded. Interactive combat is additionally covered by the existing clip/input tests.");
            File.WriteAllText(MotionPolishValidation.Out+"/Production/Full_Show_Continuity.txt",report.ToString());Debug.Log(report);
            if(invalid>0)throw new Exception("Nonfinite full-show motion");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
