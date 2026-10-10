using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using LordLyrical;
using LordLyrical.Characters;
public static class AnimationIntegration
{
    public const string Root="Assets/LordLyricalAnimation_Unity";
    public const string Output="C:/Users/kris/Documents/Codex/2026-10-03/bui/outputs/LordLyrical_Animation";
    static AnimationClip Clip(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
    [MenuItem("Lord Lyrical/Animation/Integrate Motions")]
    public static void Integrate()
    {
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Root+"/Clips");Directory.CreateDirectory("Assets/Resources/Concert");
        var library=AssetDatabase.LoadAssetAtPath<HumanoidMotionLibrary>("Assets/Resources/Concert/HumanoidMotions.asset");
        if(!library){library=ScriptableObject.CreateInstance<HumanoidMotionLibrary>();AssetDatabase.CreateAsset(library,"Assets/Resources/Concert/HumanoidMotions.asset");}
        const string basePath="Assets/Kevin Iglesias/Human Animations/Animations/Male/";
        library.idle=Clip(basePath+"Idles/HumanM@Idle01.fbx");
        library.turnLeft=Clip(basePath+"Movement/Turn/HumanM@Turn01_Left.fbx");library.turnRight=Clip(basePath+"Movement/Turn/HumanM@Turn01_Right.fbx");
        string[] dirs={"Forward","ForwardRight","Right","BackwardRight","Backward","BackwardLeft","Left","ForwardLeft"};
        for(int i=0;i<8;i++){library.walk[i]=Clip(basePath+"Movement/Walk/HumanM@Walk01_"+dirs[i]+".fbx");library.run[i]=Clip(basePath+"Movement/Run/HumanM@Run01_"+dirs[i]+".fbx");}
        EditorUtility.SetDirty(library);
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        string prefabPath=AssetDatabase.GetAssetPath(config.orcModel);var root=PrefabUtility.LoadPrefabContents(prefabPath);
        var report=new StringBuilder("OWNED MOTION INTEGRATION\n");
        try{
            var perf=root.GetComponent<SonicOrcPerformance>();
            var animator=perf.animator;
            var nodes=animator.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            AnimationClip Convert(string label,bool loop){
                string path=Root+"/Models/Orc_"+label+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.avatarSetup=ModelImporterAvatarSetup.NoAvatar;
                importer.animationCompression=ModelImporterAnimationCompression.Off;importer.SaveAndReimport();
                var source=Clip(path);string destination=Root+"/Clips/Orc_"+label+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(destination);
                if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,destination);}else clip.ClearCurves();
                clip.name="Orc_"+label;clip.frameRate=30;
                int curves=0;
                foreach(var binding in AnimationUtility.GetCurveBindings(source)){
                    if(binding.type!=typeof(Transform))continue;
                    string name=binding.path.Split('/').Last();if(!nodes.TryGetValue(name,out var node))continue;
                    var b=binding;b.path=AnimationUtility.CalculateTransformPath(node,animator.transform);
                    AnimationUtility.SetEditorCurve(clip,b,AnimationUtility.GetEditorCurve(source,binding));curves++;
                }
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
                clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);
                if(curves<100)throw new Exception("Missing Orc motion curves: "+label+" "+curves);
                report.AppendLine(label+": "+curves+" transform curves, "+clip.length+" sec, loop "+loop);return clip;
            }
            perf.idle=Convert("CombatIdle",true);perf.advance=Convert("Walk",true);perf.retreat=Convert("Run",true);
            perf.resonanceStrike=Convert("Slam",false);perf.stagger=Convert("Stagger",false);perf.defeat=Convert("Defeat",false);
            perf.sweep=Convert("Sweep",false);perf.doubleStrike=Convert("Double",false);perf.thrust=Convert("Thrust",false);perf.burst=Convert("Burst",false);perf.brace=Convert("Block",false);
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();report.AppendLine("JERU and ten participant roles load the shared Humanoid motion library at runtime.");
        File.WriteAllText(Output+"/Integration.txt",report.ToString());Debug.Log("ANIMATION_INTEGRATION_PASS\n"+report);
    }
}
