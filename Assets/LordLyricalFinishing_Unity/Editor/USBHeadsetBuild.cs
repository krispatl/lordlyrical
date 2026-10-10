using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Styly.NetSync;

public static class USBHeadsetBuild
{
    [MenuItem("Lord Lyrical/Finishing/Build Separate PICO USB Review App")]
    public static void Build()
    {
        const string path="Assets/LordLyricalFinishing_Unity/Scenes/Experience_USBReview.unity";
        if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(path))AssetDatabase.CopyAsset("Assets/LordLyrical/Scenes/Experience.unity",path);
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NetSyncManager>(true)).Single();var so=new SerializedObject(manager);so.FindProperty("_serverAddress").stringValue="127.0.0.1";so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(scene);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
        string app=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),name=PlayerSettings.productName;
        try{
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.fifthyang.lordlyrical.usbreview");PlayerSettings.productName="Lord Lyrical USB Review";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName="Builds/LordLyrical-PICO-USB-Review.apk",target=BuildTarget.Android,options=BuildOptions.Development});
            File.WriteAllText(FinishingBuilder.Output+"/USB_Build_Result.txt",$"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; bytes={report.summary.totalSize}\nSeparate application id com.fifthyang.lordlyrical.usbreview; USB reverse TCP 5555,5556,5557,8800. Production scene and settings retained.\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("USB review build failed");
        }finally{PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,app);PlayerSettings.productName=name;AssetDatabase.SaveAssets();}
    }
}
