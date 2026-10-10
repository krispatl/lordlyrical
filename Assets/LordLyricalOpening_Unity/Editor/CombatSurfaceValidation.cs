using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;

public static class CombatSurfaceValidation
{
    static void Need(bool value,string why){if(!value)throw new Exception("COMBAT SURFACE: "+why);}
    [MenuItem("Lord Lyrical/Production/Validate Combat Surface Equivalence")]
    public static void Run()
    {
        string output=MotionPolishValidation.Out+"/Production/Combat_Surface";
        Directory.CreateDirectory(output);
        var before=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene("Assets/LordLyricalOrc_Unity/Scenes/SonicOrcReview.unity",OpenSceneMode.Additive);
        var temporary=new List<Material>();
        var report=new StringBuilder("COMBAT MATERIAL AND SHADER EQUIVALENCE\n");
        try
        {
            var p=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SonicOrcPerformance>()).First();
            p.previewAutomatically=false;p.SampleClip(2,.8f);
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
            var reference=Shader.Find("Hidden/LordLyrical/Reference Resonant Actor");
            Need(reference&&reference.isSupported&&!ShaderUtil.ShaderHasError(reference),"Reference shader did not compile");
            var shader=Shader.Find("LordLyrical/Resonant Actor Surface");
            Need(shader&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"Production shader did not compile");
            var cases=new[]{(0f,0f,0f),(2.3f,0f,0f),(1.7f,.47f,0f),(2f,.32f,.75f)};
            for(int index=0;index<cases.Length;index++)
            {
                var test=cases[index];p.SetResonance(test.Item1,test.Item2);
                var renderers=p.GetComponentsInChildren<Renderer>(true);
                Need(renderers.All(r=>!r.HasPropertyBlock()),"New style disables batching");
                var saved=renderers.Select(r=>r.sharedMaterials).ToArray();
                foreach(var material in saved.SelectMany(a=>a).Distinct())if(material.HasProperty("_Fracture"))material.SetFloat("_Fracture",test.Item3);
                string current=output+"/Case_"+index+"_Current.png",legacy=output+"/Case_"+index+"_Reference.png";
                ParticipantAvatarBuilder.Render(camera,current,512,512,1);
                try
                {
                    for(int i=0;i<renderers.Length;i++)
                    {
                        var assigned=saved[i].Select(m=>new Material(m){shader=reference,hideFlags=HideFlags.HideAndDontSave}).ToArray();
                        temporary.AddRange(assigned);renderers[i].sharedMaterials=assigned;
                        var block=new MaterialPropertyBlock();
                        block.SetFloat("_Dissolve",test.Item2);
                        block.SetColor("_EmissionColor",renderers[i].name.Contains("Resonance")?new Color(1,.22f,.007f)*test.Item1:Color.black);
                        renderers[i].SetPropertyBlock(block);
                    }
                    ParticipantAvatarBuilder.Render(camera,legacy,512,512,1);
                }
                finally
                {
                    for(int i=0;i<renderers.Length;i++){renderers[i].SetPropertyBlock(null);renderers[i].sharedMaterials=saved[i];}
                }
                var a=new Texture2D(2,2);var b=new Texture2D(2,2);
                try
                {
                    a.LoadImage(File.ReadAllBytes(current));b.LoadImage(File.ReadAllBytes(legacy));
                    var ac=a.GetPixels32();var bc=b.GetPixels32();Need(ac.Length==bc.Length,"Different dimensions");
                    int maximum=0;long total=0;
                    for(int i=0;i<ac.Length;i++)
                    {
                        int r=Math.Abs(ac[i].r-bc[i].r),g=Math.Abs(ac[i].g-bc[i].g),blue=Math.Abs(ac[i].b-bc[i].b);
                        maximum=Math.Max(maximum,Math.Max(r,Math.Max(g,blue)));total+=r+g+blue;
                    }
                    double mean=total/(3.0*ac.Length);
                    report.AppendLine($"Case {index}: glow={test.Item1}, dissolve={test.Item2}, fracture={test.Item3}; maximum RGB difference={maximum}/255, mean={mean:F6}/255");
                    Need(maximum<=2&&mean<.05,"Visible material/shader regression");
                }
                finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
            }
            var second=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LordLyricalOrc_Unity/Prefabs/Sonic_Orc.prefab"));
            try
            {
                var other=second.GetComponent<SonicOrcPerformance>();other.SetResonance(.8f,.1f);p.SetResonance(2,.6f);
                var firstBody=p.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Sonic_Orc_LOD0").sharedMaterial;
                var secondBody=second.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Sonic_Orc_LOD0").sharedMaterial;
                Need(firstBody!=secondBody,"Independent Orcs share mutable materials");
                Need(Mathf.Abs(firstBody.GetFloat("_Dissolve")-.6f)<.0001f&&Mathf.Abs(secondBody.GetFloat("_Dissolve")-.1f)<.0001f,"Independent dissolve was lost");
                var asset=AssetDatabase.LoadAssetAtPath<Material>("Assets/LordLyricalOrc_Unity/Materials/Orc_Basalt_Bronze.mat");
                Need(Mathf.Abs(asset.GetFloat("_Dissolve"))<.0001f,"Source material was modified");
            }
            finally{UnityEngine.Object.DestroyImmediate(second);}
            report.AppendLine("PASS rendered equivalence for four effects combinations; per-Orc dissolve independence; source materials unchanged; no property blocks on the four Orc body/glyph renderers.");
            File.WriteAllText(output+"/Validation.txt",report.ToString());Debug.Log("COMBAT_SURFACE_EQUIVALENCE_PASS\n"+report);
        }
        finally
        {
            foreach(var material in temporary)if(material)UnityEngine.Object.DestroyImmediate(material);
            EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);
        }
    }
}
