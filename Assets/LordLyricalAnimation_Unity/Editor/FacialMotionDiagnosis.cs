using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using LordLyrical;
using LordLyrical.Characters;
public static class FacialMotionDiagnosis
{
    [MenuItem("Lord Lyrical/Animation/Diagnose Face")]
    public static void Run()
    {
        var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        var go=UnityEngine.Object.Instantiate(cfg.lordModel);
        var report=new System.Text.StringBuilder();
        try {
            foreach(var c in go.GetComponents<MonoBehaviour>()){
                var type=c.GetType();var order=type.GetCustomAttribute<DefaultExecutionOrder>();
                report.AppendLine(type.FullName+" enabled="+c.enabled+" order="+(order==null?0:order.order));
                report.AppendLine(string.Join(",",type.GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).Where(m=>m.Name=="Update"||m.Name=="LateUpdate").Select(m=>m.Name)));
            }
            foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                if(!skin.sharedMesh)continue;
                var mesh=skin.sharedMesh;report.AppendLine(skin.name+": "+string.Join(",",Enumerable.Range(0,mesh.blendShapeCount).Select(mesh.GetBlendShapeName).Where(n=>n.Contains("viseme"))));
            }
            var perf=go.GetComponent<JeruPerformance>();perf.SampleShow(34);
            perf.SetExpression("viseme_E",.6f);perf.SetExpression("viseme_aa",.4f);
            report.AppendLine("Synthetic speech before body="+Max(go));
            perf.SampleShow(34.1f);report.AppendLine("Synthetic speech after body="+Max(go));
            var feetField=typeof(JeruPerformance).GetField("feet",BindingFlags.NonPublic|BindingFlags.Instance);
            var savedFeet=feetField.GetValue(perf);
            foreach(bool raw in new[]{false,true}){
            feetField.SetValue(perf,raw?null:savedFeet);
            report.AppendLine("RAW LOCOMOTION="+raw);
            var library=HumanoidMotionLibrary.Load();report.AppendLine("WALK CLIPS: "+string.Join(",",library.walk.Select(c=>c.name)));
            for(int frame=0;frame<5;frame++){
                perf.SampleShow(frame==0?0:73+(frame-1)*.15f);var rig=perf.animator;
                var across=rig.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position-rig.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
                var facing=Vector3.Cross(Vector3.up,across).normalized;
                report.AppendLine($"Body facing {frame}: {facing}; velocity {ConductorRoute.Velocity(frame==0?0:73+(frame-1)*.15f)}");
                foreach(bool left in new[]{true,false}){
                    var hip=rig.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                    var knee=rig.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                    var foot=rig.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                    var toe=rig.GetBoneTransform(left?HumanBodyBones.LeftToes:HumanBodyBones.RightToes);
                    var forward=Vector3.ProjectOnPlane(toe.position-foot.position,Vector3.up).normalized;
                    var bend=Vector3.ProjectOnPlane(knee.position-hip.position,(foot.position-hip.position).normalized);
                    report.AppendLine($"Knee {frame} {left}: toeForward={forward} bend={bend} dot={Vector3.Dot(forward,bend):F4}");
                }
            }
            }
            var face=go.GetComponent<JeruConcertFace>();face.Sample(34,true);report.AppendLine("Vocal signal at 34s="+face.VocalActivity);
            File.WriteAllText(MotionPolishValidation.Out+"/Facial_Diagnosis.txt",report.ToString());Debug.Log("FACIAL_DIAGNOSIS_COMPLETE");
        } finally {UnityEngine.Object.DestroyImmediate(go);}
    }
    static float Max(GameObject go){float peak=0;foreach(var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(s.sharedMesh)for(int i=0;i<s.sharedMesh.blendShapeCount;i++)if(s.sharedMesh.GetBlendShapeName(i).Contains("viseme"))peak=Mathf.Max(peak,s.GetBlendShapeWeight(i));return peak;}
}
