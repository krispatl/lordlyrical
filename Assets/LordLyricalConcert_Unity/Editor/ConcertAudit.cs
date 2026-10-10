using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using LordLyrical;

public static class ConcertAudit
{
    static object PackageService(string name){var a=typeof(Editor).Assembly;var t=a.GetType("UnityEditor.PackageManager.UI.Internal.ServicesContainer");var s=Resources.FindObjectsOfTypeAll(t).First();return t.GetMethod("Resolve").MakeGenericMethod(a.GetType("UnityEditor.PackageManager.UI.Internal."+name)).Invoke(s,null);}
    [MenuItem("Lord Lyrical/Concert/Download Owned Selections")]
    public static void DownloadOwned(){var m=PackageService("IAssetStoreDownloadManager");Debug.Log("OWNED_DOWNLOAD "+m.GetType().GetMethod("Download").Invoke(m,new object[]{new long[]{148442,176571,84617}}));}
    [MenuItem("Lord Lyrical/Concert/Owned Download Status")]
    public static void DownloadStatus(){var m=PackageService("IAssetStoreDownloadManager");var cache=PackageService("IAssetStoreCache");var b=new StringBuilder();foreach(long id in new long[]{148442,176571,84617}){var op=m.GetType().GetMethod("GetDownloadOperation").Invoke(m,new object[]{id});if(op!=null)foreach(string p in new[]{"productId","state","progressPercentage","errorMessage","packageNewPath"})b.AppendLine(p+": "+op.GetType().GetProperty(p).GetValue(op));var local=cache.GetType().GetMethod("GetLocalInfo").Invoke(cache,new object[]{id});if(local!=null)b.AppendLine(id+" LOCAL "+local.GetType().GetField("packagePath").GetValue(local));}File.WriteAllText(Output+"/Owned_Download_Status.txt",b.ToString());}
    [MenuItem("Lord Lyrical/Concert/Package API Contract")]
    public static void PackageContract()
    {
        var b=new StringBuilder();var a=typeof(Editor).Assembly;
        foreach(var t in a.GetTypes().Where(t=>new[]{"AssetStoreDownloadManager","AssetStoreDownloadOperation","AssetStoreCache","AssetStoreLocalInfo","AssetStoreProductInfo","ServicesContainer"}.Contains(t.Name))) {
            b.AppendLine(t.FullName);
            foreach(var f in t.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic))if(!t.Name.Contains("Services"))b.AppendLine("FIELD "+f.FieldType+" "+f.Name);
            foreach(var m in t.GetMethods().Where(m=>m.DeclaringType==t))b.AppendLine(m.ToString());
        }
        File.WriteAllText(Output+"/Package_Api.txt",b.ToString());
    }
    public const string Output=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\LordLyrical_Concert";
    [MenuItem("Lord Lyrical/Concert/Read Owned Package List")]
    public static void ReadPackages()
    {
        var w=Resources.FindObjectsOfTypeAll<EditorWindow>().First(x=>x.GetType().Name=="PackageManagerWindow");
        var labels=w.rootVisualElement.Query<TextElement>().ToList().Select(x=>x.text).Where(s=>!string.IsNullOrWhiteSpace(s)).Distinct();
        File.WriteAllLines(Output+"/Owned_Package_Manager_Labels.txt",labels);
        var b=new StringBuilder(w.GetType().Assembly.FullName+"\n");foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())if(assembly==w.GetType().Assembly||assembly.GetName().Name.Contains("UnityEditor")){
            foreach(var type in assembly.GetTypes().Where(t=>t.Name=="ServicesContainer"||t.Name=="AssetStoreCache"||t.Name=="AssetStorePurchaseInfo")){
                b.AppendLine(type.FullName);
                foreach(var f in type.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic))b.AppendLine("FIELD "+f.FieldType.Name+" "+f.Name);
                foreach(var p in type.GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic))b.AppendLine("PROPERTY "+p.PropertyType.Name+" "+p.Name);
                foreach(var m in type.GetMethods().Where(m=>m.Name.Contains("Resolve")||m.Name.Contains("Purchase")))b.AppendLine("METHOD "+m);
            }
        }
        File.WriteAllText(Output+"/Package_Metadata_Contract.txt",b.ToString());
        var servicesType=w.GetType().Assembly.GetType("UnityEditor.PackageManager.UI.Internal.ServicesContainer");
        var services=Resources.FindObjectsOfTypeAll(servicesType).First();
        var cache=servicesType.GetField("m_SerializedAssetStoreCache",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(services);
        var purchases=(System.Collections.IDictionary)cache.GetType().GetField("m_PurchaseInfos",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(cache);
        var names=new System.Collections.Generic.List<string>();foreach(var item in purchases.Values){var type=item.GetType();names.Add(type.GetField("productId").GetValue(item)+"\t"+type.GetField("displayName").GetValue(item));}
        File.WriteAllLines(Output+"/Owned_Assets.tsv",names.OrderBy(s=>s.Split('\t')[1]));
        Debug.Log("OWNED_ASSET_LIST_CAPTURED");
    }
    [MenuItem("Lord Lyrical/Concert/Audit Existing Experience")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);var b=new StringBuilder("LORD LYRICAL PROJECT AUDIT\n"+DateTime.Now+"\nUnity "+Application.unityVersion+"\n");
        foreach(string type in new[]{"Scene","Prefab","AnimationClip","AnimatorController","Material","Shader","AudioClip","Mesh","NavMeshData"})b.AppendLine(type+" assets: "+AssetDatabase.FindAssets("t:"+type,new[]{"Assets"}).Length);
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        foreach(var prefab in new[]{config.lordModel,config.orcModel,config.dragonModel,config.chamberModel}){
            b.AppendLine("\nPREFAB "+AssetDatabase.GetAssetPath(prefab));var skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            b.AppendLine("Skins="+skins.Length+" static renderers="+filters.Length+" vertices="+(skins.Sum(s=>s.sharedMesh?s.sharedMesh.vertexCount:0)+filters.Sum(s=>s.sharedMesh?s.sharedMesh.vertexCount:0)));
            foreach(var a in prefab.GetComponentsInChildren<Animator>(true))b.AppendLine("RIG "+a.name+" human="+a.isHuman+" controller="+(a.runtimeAnimatorController?a.runtimeAnimatorController.name:"none"));
            foreach(var s in skins)if(s.sharedMesh&&s.sharedMesh.blendShapeCount>0)b.AppendLine("FACE "+s.name+": "+string.Join(", ",Enumerable.Range(0,s.sharedMesh.blendShapeCount).Select(s.sharedMesh.GetBlendShapeName)));
            foreach(var c in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(prefab)).Where(p=>p.EndsWith(".anim"))) {var a=AssetDatabase.LoadAssetAtPath<AnimationClip>(c);b.AppendLine("CLIP "+a.name+" "+a.length+"s");}
        }
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var session=new GameObject("audit network").AddComponent<NetworkSessionController>();var world=new GameObject("audit world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            foreach(float t in new[]{0f,21f,60f,66f,75f,99f,117f}){
                world.Render(ShowFrame.At(t),true);var ts=world.GetComponentsInChildren<Transform>(true);
                b.AppendLine("\nAT "+t+"s: renderers="+world.GetComponentsInChildren<Renderer>(false).Length+" dragon roots="+string.Join(" | ",ts.Where(x=>x.name.Contains("Dragon")||x.name.Contains("dragon")).Select(x=>x.name+" active="+x.gameObject.activeInHierarchy)));
            }
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        File.WriteAllText(Output+"/Existing_Project_Audit.txt",b.ToString());Debug.Log("CONCERT_AUDIT_COMPLETE");
    }
}
