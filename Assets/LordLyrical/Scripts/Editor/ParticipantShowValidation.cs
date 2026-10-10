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

public static class ParticipantShowValidation
{
    [MenuItem("Lord Lyrical/Participants/Connect and Verify Complete Show")]
    public static void ConnectAndVerify()
    {
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        var prefabs=ParticipantAvatarBuilder.Ids.Select(id=>AssetDatabase.LoadAssetAtPath<GameObject>(ParticipantAvatarBuilder.Root+"/Prefabs/"+id+".prefab")).ToArray();
        if(prefabs.Any(p=>!p||!p.GetComponent<ParticipantAvatarPerformance>()))throw new Exception("Ten rigged participant prefabs are required.");
        var oldDragon=config.dragonModel;var oldLord=config.lordModel;var oldOrc=config.orcModel;var oldChamber=config.chamberModel;
        Undo.RecordObject(config,"Connect authored participant roster");config.participantModels=prefabs;EditorUtility.SetDirty(config);
        foreach(string path in new[]{"Assets/LordLyrical/Prefabs/Network/LocalAvatar.prefab","Assets/LordLyrical/Prefabs/Network/RemoteAvatar.prefab"}){
            var root=PrefabUtility.LoadPrefabContents(path);
            try{root.GetComponent<NetworkRoleAvatar>().participantModels=prefabs;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        if(config.dragonModel!=oldDragon||config.lordModel!=oldLord||config.orcModel!=oldOrc||config.chamberModel!=oldChamber)throw new Exception("Existing cast references changed.");
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var session=new GameObject("Participant validation session").AddComponent<NetworkSessionController>();var world=new GameObject("Full-show participant validation").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var roles=world.GetComponentsInChildren<ParticipantAvatarPerformance>(true).OrderBy(p=>p.slot).ToArray();if(roles.Length!=10)throw new Exception("Expected ten role actors, found "+roles.Length);
            if(!world.GetComponentInChildren<JeruPerformance>(true)||!world.GetComponentInChildren<ChamberPresentation>(true)||world.GetComponentsInChildren<SonicOrcPerformance>(true).Length!=12)throw new Exception("Existing chamber, Jeru or twelve Orcs missing.");
            var report=new StringBuilder("TEN AVATAR SHOW INTEGRATION\nPASS ten role prefabs bound to Experience and both network avatar prefabs\nPASS existing Jeru, Orc, dragon and chamber references preserved\n");
            var camera=new GameObject("Participant chamber camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,3.4f,9);camera.transform.LookAt(new Vector3(0,1,-1));camera.fieldOfView=68;camera.cullingMask=1<<29;camera.backgroundColor=new Color(.006f,.009f,.014f);camera.clearFlags=CameraClearFlags.SolidColor;
            var key=new GameObject("Participant review key").AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(30,165,0);key.intensity=1.8f;key.color=new Color(1,.93f,.83f);
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
            foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)))light.cullingMask=1<<29;
            Vector3[] first=null;
            foreach(float time in new[]{24,31,39.5f,43.5f,48,52.5f,74.4f,81.5f,90.8f,96.2f,101,31}){
                world.Render(ShowFrame.At(time),true);
                foreach(var p in roles)if(!p.animator.avatar.isHuman||!p.animator.avatar.isValid)throw new Exception("Invalid participant avatar in show");
                if(time==24)ParticipantAvatarBuilder.Render(camera,ParticipantAvatarBuilder.Output+"/Avatars_In_Chamber.png",1700,1050,1);
                if(time==31){var mesh=new Mesh();roles[0].bodyLevels[0].BakeMesh(mesh);if(first==null)first=mesh.vertices;else{float error=first.Zip(mesh.vertices,(a,b)=>Vector3.Distance(a,b)).Max();if(error>.0001f)throw new Exception("Repeated full-show seek mismatch "+error);report.AppendLine("PASS repeated full-show seek error "+error+" metres");}UnityEngine.Object.DestroyImmediate(mesh);}
                report.AppendLine("PASS "+time+" s: ten role rigs; clips "+string.Join(",",roles.Select(p=>p.CurrentClip))+"; Shadow hidden "+roles[5].IsNarrativelyHidden);
            }
            // A local viewer sees articulated hands and never their own head/torso.
            var local=new RoleVisual(world.transform,0,prefabs[0]);local.Render(ShowFrame.At(31),true,true);if(local.root.gameObject.activeSelf)throw new Exception("Local cinematic duplicate is visible");UnityEngine.Object.DestroyImmediate(local.root.gameObject);
            LordLyrical.Editor.ShowVerification.VerifyCore();report.AppendLine("PASS local cinematic duplicate hidden and existing core clock/cue/role tests");
            File.WriteAllText(ParticipantAvatarBuilder.Output+"/Show_Integration_Validation.txt",report.ToString());Debug.Log("TEN_AVATARS_SHOW_PASS\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}
