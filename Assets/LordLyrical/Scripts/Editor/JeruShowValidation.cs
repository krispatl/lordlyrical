using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;

public static class JeruShowValidation
{
    [MenuItem("Lord Lyrical/Jeru/Verify Show Connection")]
    public static void Verify()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Jeru test session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Jeru show test").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var report="Configured prefab: "+AssetDatabase.GetAssetPath(config.lordModel)+"\n";
            foreach(float time in new[]{0,21,59.5f,74,96,117,59.5f})
            {
                world.Render(ShowFrame.At(time),true);
                var p=world.GetComponentInChildren<JeruPerformance>(true);if(!p)throw new Exception("Rigged Jeru was not instantiated.");
                var renderers=p.GetComponentsInChildren<SkinnedMeshRenderer>();if(renderers.Length!=42||p.GetComponent<LODGroup>().lodCount!=3)throw new Exception("Unexpected Jeru renderer count.");
                if(renderers.Any(r=>!r.gameObject.activeInHierarchy))throw new Exception("Jeru renderer is disabled.");
                var face=renderers.First(r=>r.name.Contains("Jeru_Face"));
                report+="PASS "+time+"s: "+renderers.Length+" skinned LOD representations; face bounds="+face.bounds+"; jaw="+face.GetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex("jawOpen"))+"\n";
            }
            File.WriteAllText(@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\Jeru_Likeness\Show_Connection_Validation.txt",report);
            Debug.Log("JERU_SHOW_CONNECTION_PASS\n"+report);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
