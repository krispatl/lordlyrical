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
public static class MotionDiagnosis
{
    const string Out="C:/Users/kris/Documents/Codex/2026-10-03/bui/outputs/LordLyrical_Animation_Polish";
    [MenuItem("Lord Lyrical/Animation/Diagnose Actual Pose")]
    public static void Run()
    {
        var before=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var report=new StringBuilder();
        try {
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var orc=UnityEngine.Object.Instantiate(cfg.orcModel);var perf=orc.GetComponent<SonicOrcPerformance>();
            var a=perf.animator;
            report.AppendLine($"Orc Animator enabled={a.enabled} human={a.isHuman} controller={a.runtimeAnimatorController?.name} root={a.name}");
            var bones=a.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="calf_l"||t.name=="calf_r"||t.name=="upperarm_l"||t.name=="upperarm_r"||t.name=="hand_l").ToArray();
            for(int clip=0;clip<11;clip++){
                perf.SampleClip(clip,.08f);var first=bones.Select(t=>t.localRotation).ToArray();
                perf.SampleClip(clip,.48f);var second=bones.Select(t=>t.localRotation).ToArray();
                a.Update(.016f);var automatic=bones.Select(t=>t.localRotation).ToArray();
                report.AppendLine($"Orc clip {clip}: manual delta={first.Select((q,i)=>Quaternion.Angle(q,second[i])).Max():F2} deg; automatic overwrite={second.Select((q,i)=>Quaternion.Angle(q,automatic[i])).Max():F2} deg");
            }
            var jeru=UnityEngine.Object.Instantiate(cfg.lordModel).GetComponent<JeruPerformance>();var ja=jeru.animator;
            report.AppendLine($"Jeru Animator enabled={ja.enabled} human={ja.isHuman} controller={ja.runtimeAnimatorController?.name}");
            var jnodes=new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm}.Select(ja.GetBoneTransform).ToArray();
            jeru.SampleShow(73);var j1=jnodes.Select(t=>t.localRotation).ToArray();jeru.SampleShow(73.3f);var j2=jnodes.Select(t=>t.localRotation).ToArray();ja.Update(.016f);
            report.AppendLine($"Jeru manual delta={j1.Select((q,i)=>Quaternion.Angle(q,j2[i])).Max():F2} deg; automatic overwrite={j2.Select((q,i)=>Quaternion.Angle(q,jnodes[i].localRotation)).Max():F2} deg");
            var dragon=UnityEngine.Object.Instantiate(cfg.dragonModel);
            report.AppendLine("DRAGON TRANSFORMS\n"+string.Join("\n",dragon.GetComponentsInChildren<Transform>(true).Select(t=>$"{t.name}: pos={t.localPosition:F3} rot={t.localEulerAngles:F2} parent={t.parent?.name}")));
            var chamber=UnityEngine.Object.Instantiate(cfg.chamberModel).GetComponentInChildren<ChamberPresentation>(true);
            foreach(var c in new[]{chamber.unfoldClip,chamber.glideClip,chamber.flyClip,chamber.roarClip})report.AppendLine($"DRAGON CLIP {c?.name} duration={c?.length} bindings={(c?AnimationUtility.GetCurveBindings(c).Length:0)}");
            File.WriteAllText(Out+"/Motion_Diagnosis.txt",report.ToString());Debug.Log("MOTION_DIAGNOSIS_COMPLETE\n"+report);
        } finally {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}