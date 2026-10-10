using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical.Characters;

public static class IntactOrcReview
{
    static void Need(bool value,string reason){if(!value)throw new Exception("INTACT ORC: "+reason);}
    [MenuItem("Lord Lyrical/Production/Validate Intact Orc Rendering")]
    public static void Run()
    {
        string output=MotionPolishValidation.Out+"/Production/IntactOrc21";
        Directory.CreateDirectory(output);
        var before=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene("Assets/LordLyricalOrc_Unity/Scenes/SonicOrcReview.unity",OpenSceneMode.Additive);
        var temporary=new List<Material>();
        var report=new StringBuilder("INTACT ORC SHADER / RENDERED COMPARISON\n");
        try {
            var actor=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SonicOrcPerformance>()).First();
            actor.previewAutomatically=false;
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
            var original=Shader.Find("LordLyrical/Resonant Actor Surface");
            var intact=Resources.Load<Shader>("LordLyrical/IntactActorSurface");
            Need(original&&intact&&original.isSupported&&intact.isSupported,"Surface shaders unavailable");
            Need(!ShaderUtil.ShaderHasError(original)&&!ShaderUtil.ShaderHasError(intact),"Shader compilation failed");
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            float[] dissolves={0,.0001f,.00011f,.47f,.9999f,1,0};
            int pairs=0,maxPixel=0;double worstMean=0;int minimumIntactMaterials=int.MaxValue;
            for(int view=0;view<3;view++){
                camera.transform.SetPositionAndRotation(position,rotation);
                camera.transform.RotateAround(actor.transform.position+Vector3.up*1.4f,Vector3.up,view==0?0:view==1?65:160);
                actor.SampleClip(view==0?0:view==1?2:1,view==0?.7f:view==1?.8f:.4f,0,true);
                for(int step=0;step<dissolves.Length;step++){
                    float dissolve=dissolves[step],strength=step%2==0?1.2f:3.5f;
                    actor.SetResonance(strength,dissolve);
                    var renderers=actor.GetComponentsInChildren<Renderer>(true);
                    Need(renderers.All(r=>!r.HasPropertyBlock()),"Unexpected property blocks");
                    var saved=renderers.Select(r=>r.sharedMaterials).ToArray();
                    var styles=saved.SelectMany(m=>m).Where(m=>m&&(m.shader==intact||m.shader==original)).Distinct().ToArray();
                    Need(styles.Length>0,"No actor materials exercised");
                    bool shouldBeIntact=dissolve<=.0001f;
                    Need(styles.All(m=>m.shader==(shouldBeIntact?intact:original)),"Wrong shader after dissolve transition");
                    if(shouldBeIntact)minimumIntactMaterials=Math.Min(minimumIntactMaterials,styles.Length);
                    foreach(var m in styles)if(m.HasProperty("_Fracture"))m.SetFloat("_Fracture",view==2?.55f:0);
                    string id="View_"+view+"_State_"+step;
                    string current=output+"/"+id+"_Current.png",reference=output+"/"+id+"_Reference.png";
                    ParticipantAvatarBuilder.Render(camera,current,512,512,1);
                    try {
                        for(int i=0;i<renderers.Length;i++){
                            var assigned=saved[i].Select(m=>{
                                if(!m||m.shader!=intact)return m;
                                var copy=new Material(m){shader=original,hideFlags=HideFlags.HideAndDontSave};
                                temporary.Add(copy);return copy;
                            }).ToArray();
                            renderers[i].sharedMaterials=assigned;
                        }
                        ParticipantAvatarBuilder.Render(camera,reference,512,512,1);
                    } finally {
                        for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=saved[i];
                        foreach(var material in temporary)if(material)UnityEngine.Object.DestroyImmediate(material);
                        temporary.Clear();
                    }
                    var a=new Texture2D(2,2);var b=new Texture2D(2,2);
                    try {
                        a.LoadImage(File.ReadAllBytes(current));b.LoadImage(File.ReadAllBytes(reference));
                        var ac=a.GetPixels32();var bc=b.GetPixels32();Need(ac.Length==bc.Length,"Image dimensions differ");
                        int peak=0;long total=0;
                        for(int i=0;i<ac.Length;i++){
                            int dr=Math.Abs(ac[i].r-bc[i].r),dg=Math.Abs(ac[i].g-bc[i].g),db=Math.Abs(ac[i].b-bc[i].b);
                            peak=Math.Max(peak,Math.Max(dr,Math.Max(dg,db)));total+=dr+dg+db;
                        }
                        double mean=total/(3.0*ac.Length);maxPixel=Math.Max(maxPixel,peak);worstMean=Math.Max(worstMean,mean);
                        Need(peak<=2&&mean<.05,"Visible difference: "+id+" peak="+peak+" mean="+mean);
                        report.AppendLine($"{id}: dissolve={dissolve:F5}, emission={strength:F1}, maximum RGB delta={peak}/255, mean={mean:F8}/255");
                        pairs++;
                    } finally {UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
                }
            }
            Need(pairs==21,"Incomplete render comparisons");
            Need(AssetDatabase.LoadAssetAtPath<Material>("Assets/LordLyricalOrc_Unity/Materials/Orc_Basalt_Bronze.mat").shader==original,"Source material changed");
            report.AppendLine($"PASS {pairs} render pairs; worst maximum RGB difference {maxPixel}/255, worst mean {worstMean:F8}/255; minimum {minimumIntactMaterials} actor materials on intact path.");
            report.AppendLine("PASS threshold crossing, complete dissolve, return to intact, front/side/back poses, glow/fracture, batching-compatible materials, source material preserved.");
            report.AppendLine("SCOPE: editor image equivalence and shader selection. Native GPU improvement is unmeasured; requires an equivalent worn-headset run.");
            File.WriteAllText(output+"/Validation.txt",report.ToString());
            Debug.Log("INTACT_ORC_REVIEW_PASS\n"+report);
        } finally {
            foreach(var m in temporary)if(m)UnityEngine.Object.DestroyImmediate(m);
            EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);
        }
    }
}
