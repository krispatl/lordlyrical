using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using LordLyrical;
using Styly.NetSync;
public static class OfflineDemoBuilder
{
    const string Scene="Assets/LordLyrical/Scenes/OfflineDemo.unity";
    static string Out=>MotionPolishValidation.Out+"/Production/OfflineDemo"+PlayerSettings.Android.bundleVersionCode;
    static void Need(bool ok,string message){if(!ok)throw new Exception("OFFLINE: "+message);}
    public static void Build()
    {
        Directory.CreateDirectory(Out);
        var test=new GameObject("Offline validation");var s=test.AddComponent<NetworkSessionController>();s.offlineDemo=true;s.InitializeOffline();
        Need(s.Ready&&s.Snapshot.IsValid&&s.Manager==null&&s.Slot==0,"Local readiness");
        s.AdvanceOffline(100,true);Need(s.Snapshot.phase=="Ready","Started before alignment");
        s.RoomAligned=true;s.AdvanceOffline(101,false);Need(s.Snapshot.phase=="Ready","Started while unfocused");
        s.AdvanceOffline(102,true);Need(s.Snapshot.epoch==119&&s.Snapshot.phase=="Countdown","Opening timing");
        s.AdvanceOffline(110,true);Need(s.Snapshot.epoch==119&&s.Snapshot.runId==1,"Repeated start");
        s.ResetClock();Need(s.Ready&&s.Snapshot.epoch==119,"Local resume clock");
        s.AdvanceOffline(119,true);Need(s.Snapshot.phase=="Running","Music start");
        s.AdvanceOffline(240,true);Need(s.Snapshot.phase=="Complete","Completion");
        s.Snapshot.epoch=s.ServerNow-CinemaTiming.OutroComplete-.1;Need(s.CanReplayOffline,"Replay not available after farewell");s.ReplayOffline();s.AdvanceOffline(300,true);Need(s.Snapshot.runId==2&&s.Snapshot.epoch==317,"Replay failed");
        UnityEngine.Object.DestroyImmediate(test);
        var original=EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);
        EditorSceneManager.SaveScene(original,Scene,true);var offline=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
        foreach(var manager in UnityEngine.Object.FindObjectsByType<NetSyncManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(manager.gameObject);
        var session=UnityEngine.Object.FindFirstObjectByType<NetworkSessionController>();session.offlineDemo=true;EditorUtility.SetDirty(session);
        Need(UnityEngine.Object.FindObjectsByType<NetSyncManager>(FindObjectsSortMode.None).Length==0,"Network manager in offline scene");
        EditorSceneManager.SaveScene(offline);
        File.WriteAllText(Out+"/Validation.txt","PASS local assignment, no network manager, no start before alignment/focus, one autoplay start, 17-second opening, local clock recovery, music and completion. Combat uses existing local authority path. Physical headset acceptance pending.\n");
        var id=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);var title=PlayerSettings.productName;
        try{
            LordLyrical.Editor.ShowBuilder.ConfigureAndroid();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.fifthyang.lordlyrical.offline");PlayerSettings.productName="Lord Lyrical Offline Demo";AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/LordLyrical-OfflineDemo-PICO.apk",target=BuildTarget.Android,options=BuildOptions.None});
            File.WriteAllText(Out+"/Build.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
            Need(report.summary.result==BuildResult.Succeeded,"Build failed");
        }finally{PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,id);PlayerSettings.productName=title;AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);}
    }
}
