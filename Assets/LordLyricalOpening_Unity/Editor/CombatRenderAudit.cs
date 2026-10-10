using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using LordLyrical.Characters;

public static class CombatRenderAudit
{
    [MenuItem("Lord Lyrical/Production/Audit Combat Rendering")]
    public static void Run()
    {
        var output=Path.Combine(MotionPolishValidation.Out,"Production","Combat_Render_Audit.txt");
        var report=new StringBuilder("ORC RENDERING AUDIT\n");
        var shader=Shader.Find("LordLyrical/Resonant Actor Surface");
        foreach(var method in typeof(ShaderUtil).GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)
                .Where(m=>m.Name.Contains("SRPBatcherCompatibility")))
        {
            report.AppendLine(method.ToString());
            var args=method.GetParameters();
            if(args.Length==2 && args[0].ParameterType==typeof(Shader) && args[1].ParameterType==typeof(int))
            {
                try{ report.AppendLine("Result: "+method.Invoke(null,new object[]{shader,0})); }
                catch(Exception ex){report.AppendLine("Unavailable: "+ex.GetBaseException().Message);}
            }
        }
        var root=PrefabUtility.LoadPrefabContents("Assets/LordLyricalOrc_Unity/Prefabs/Sonic_Orc.prefab");
        try
        {
            var performance=root.GetComponent<SonicOrcPerformance>();
            var renderers=root.GetComponentsInChildren<Renderer>(true);
            report.AppendLine("Renderer count per Orc including all LODs: "+renderers.Length);
            report.AppendLine("Initial property-block renderers: "+renderers.Count(r=>r.HasPropertyBlock()));
            performance.SetResonance(1.3f,0);
            report.AppendLine("After normal resonance: "+renderers.Count(r=>r.HasPropertyBlock())+" property-block renderers");
            foreach(var r in renderers)
                report.AppendLine(r.name+" | propertyBlock="+r.HasPropertyBlock()+" | "+string.Join(", ",r.sharedMaterials.Select(m=>m?m.name+" / "+m.shader.name:"null")));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        File.WriteAllText(output,report.ToString());
        Debug.Log("COMBAT_RENDER_AUDIT\n"+report);
    }
}
