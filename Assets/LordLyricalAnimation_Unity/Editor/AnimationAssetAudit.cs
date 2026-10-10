using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class AnimationAssetAudit
{
    public const string Output=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\LordLyrical_Animation";
    static readonly long[] Products={154271,25900,631};
    static object Service(string name){var a=typeof(Editor).Assembly;var t=a.GetType("UnityEditor.PackageManager.UI.Internal.ServicesContainer");var s=Resources.FindObjectsOfTypeAll(t).First();return t.GetMethod("Resolve").MakeGenericMethod(a.GetType("UnityEditor.PackageManager.UI.Internal."+name)).Invoke(s,null);}
    [MenuItem("Lord Lyrical/Animation/Download Owned Motions")]
    public static void Download(){var m=Service("IAssetStoreDownloadManager");Debug.Log("ANIMATION_DOWNLOAD "+m.GetType().GetMethod("Download").Invoke(m,new object[]{Products}));}
    [MenuItem("Lord Lyrical/Animation/Download Status")]
    public static void Status(){Directory.CreateDirectory(Output);var m=Service("IAssetStoreDownloadManager");var c=Service("IAssetStoreCache");var b=new StringBuilder();foreach(long id in Products){b.AppendLine("PRODUCT "+id);var op=m.GetType().GetMethod("GetDownloadOperation").Invoke(m,new object[]{id});if(op!=null)foreach(string p in new[]{"state","progressPercentage","errorMessage","packageNewPath"})b.AppendLine(p+": "+op.GetType().GetProperty(p).GetValue(op));var local=c.GetType().GetMethod("GetLocalInfo").Invoke(c,new object[]{id});if(local!=null)b.AppendLine("LOCAL "+local.GetType().GetField("packagePath").GetValue(local));}File.WriteAllText(Output+"/Owned_Motion_Downloads.txt",b.ToString());}
    [MenuItem("Lord Lyrical/Animation/Audit Clips")]
    public static void Clips(){Directory.CreateDirectory(Output);var b=new StringBuilder();foreach(var path in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Distinct())foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__preview__")))b.AppendLine(path+"\t"+clip.name+"\t"+clip.length+"\t"+clip.isHumanMotion+"\t"+clip.averageSpeed+"\t"+clip.isLooping);File.WriteAllText(Output+"/Clip_Inventory.tsv",b.ToString());Debug.Log("ANIMATION_CLIP_AUDIT_COMPLETE");}
}
