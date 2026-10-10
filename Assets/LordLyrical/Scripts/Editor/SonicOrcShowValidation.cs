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

public static class SonicOrcShowValidation
{
    const string Output=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\Sonic_Orc";
    [MenuItem("Lord Lyrical/Sonic Orc/Connect and Verify Show")]
    public static void Verify()
    {
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LordLyricalOrc_Unity/Prefabs/Sonic_Orc.prefab");
        if(!prefab||!prefab.GetComponent<SonicOrcPerformance>())throw new Exception("Rigged Orc prefab missing.");
        var so=new SerializedObject(config);so.FindProperty("orcModel").objectReferenceValue=prefab;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var session=new GameObject("Orc validation session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Orc chamber encounter validation").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var camera=new GameObject("Orc encounter camera").AddComponent<Camera>();camera.transform.position=new Vector3(7,3.2f,8);camera.transform.LookAt(new Vector3(0,1.4f,16.2f));camera.fieldOfView=62;camera.cullingMask=1<<29;camera.backgroundColor=new Color(.006f,.009f,.014f);camera.clearFlags=CameraClearFlags.SolidColor;
            var key=new GameObject("Encounter review key light").AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(35,-25,0);key.color=new Color(1,.85f,.67f);key.intensity=1.2f;
            foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
            foreach(var l in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)))l.cullingMask=1<<29;
            var actors=world.GetComponentsInChildren<SonicOrcPerformance>(true);if(actors.Length!=12)throw new Exception("Expected twelve authored Orcs, found "+actors.Length);
            var report=new StringBuilder("SONIC ORC SHOW INTEGRATION\n");report.AppendLine("Configured prefab: "+AssetDatabase.GetAssetPath(config.orcModel));
            Vector3[] first=null;
            foreach(float time in new[]{21,31,39.3f,40,45.6f,47.5f,51,54,31})
            {
                world.Render(ShowFrame.At(time),true);int active=actors.Count(p=>p.gameObject.activeInHierarchy);int expected=time<26||time>=54?0:time>=39.72f?11:12;
                if(active!=expected)throw new Exception("Orc visibility at "+time+": "+active+"; expected "+expected);
                foreach(var p in actors)
                {
                    if(p.transform.localScale!=Vector3.one)throw new Exception("Authored Orc scale changed.");
                    if(p.GetComponent<LODGroup>().lodCount!=3)throw new Exception("Missing LODs.");
                    if(!p.GetComponentsInChildren<SkinnedMeshRenderer>(true).All(r=>r.sharedMaterial.shader.name=="LordLyrical/Resonant Actor Surface"))throw new Exception("Authored Orc materials replaced.");
                    if(!p.leftWeaponSocket.Find("Tuning fork club"))throw new Exception("Weapon is not attached to hand.");
                }
                if(time==31)
                {
                    var skin=actors[0].GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Sonic_Orc_LOD0");var mesh=new Mesh();skin.BakeMesh(mesh);var points=mesh.vertices;
                    if(first==null){first=points;SonicOrcBuilder.Render(camera,Output+"/Sonic_Orcs_In_Chamber.png",1400,1);}
                    else{float error=0;for(int i=0;i<points.Length;i++)error=Mathf.Max(error,Vector3.Distance(first[i],points[i]));if(error>.0001f)throw new Exception("Show seek pose mismatch: "+error);report.AppendLine("PASS full-show repeated seek vertex error: "+error+" m");}
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                report.AppendLine("PASS "+time+" s: "+active+" active authored Orcs; clips "+string.Join(",",actors.Where(p=>p.gameObject.activeInHierarchy).Select(p=>p.CurrentClip)));
            }
            if(!world.GetComponentInChildren<JeruPerformance>(true))throw new Exception("Jeru is missing.");
            if(!world.GetComponentInChildren<ChamberPresentation>(true))throw new Exception("Chamber is missing.");
            LordLyrical.Editor.ShowVerification.VerifyCore();report.AppendLine("PASS existing core cue, clock, key and reconstruction tests; Jeru and authored chamber present.");
            File.WriteAllText(Output+"/Show_Integration_Validation.txt",report.ToString());Debug.Log("SONIC_ORC_SHOW_PASS\n"+report);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}
