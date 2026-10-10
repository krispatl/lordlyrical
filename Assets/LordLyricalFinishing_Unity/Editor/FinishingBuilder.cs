using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LordLyrical;
using LordLyrical.Characters;

public static class FinishingBuilder
{
    const string Root="Assets/LordLyricalFinishing_Unity";
    public const string Output=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\LordLyrical_Finishing";
    [Serializable]class Mat {public string name;public float[] color,emission;public float metallic,roughness,strength;}
    [Serializable]class Manifest {public Mat[] materials;}
    static Color C(float[] a)=>new Color(a[0],a[1],a[2],a.Length>3?a[3]:1);
    static void Require(bool pass,string message){if(!pass)throw new Exception("FINISHING: "+message);}
    static Material Material(string name,string shader){string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}m.shader=Shader.Find(shader);return m;}
    [MenuItem("Lord Lyrical/Finishing/Build and Connect All Assets")]
    public static void Build()
    {
        foreach(string p in new[]{"Materials","Prefabs","Scenes","Audio","Voice"})Directory.CreateDirectory(Root+"/"+p);AssetDatabase.Refresh();
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/art_manifest.json"));var mats=new Dictionary<string,Material>();
        foreach(var data in manifest.materials){
            bool filament=data.name.StartsWith("Vortex_")&&data.strength>0;
            var m=Material(data.name,filament?"LordLyrical/Resonant Filament":"Universal Render Pipeline/Lit");
            if(filament){m.SetColor("_Color",C(data.color));m.SetFloat("_Gain",.85f);m.SetFloat("_Mode",1);}
            else{m.SetColor("_BaseColor",C(data.color));m.SetFloat("_Metallic",data.metallic);m.SetFloat("_Smoothness",1-data.roughness);if(data.strength>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",C(data.emission)*data.strength);}}
            EditorUtility.SetDirty(m);mats.Add(data.name,m);
        }
        var energy=Material("Resonant_Filament","LordLyrical/Resonant Filament");energy.SetColor("_Color",Color.white);energy.SetFloat("_Gain",1);energy.SetFloat("_Mode",0);EditorUtility.SetDirty(energy);
        foreach(string file in new[]{"Double_Headed_Tuning_Fork","Frequency_Vortex"}){
            var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/"+file+".fbx");importer.importAnimation=false;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var entry in mats)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),entry.Key),entry.Value);importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+file+".fbx");var go=(GameObject)PrefabUtility.InstantiatePrefab(model);
            try{
                go.name=file;
                if(file.StartsWith("Double")){
                    var renderers=go.GetComponentsInChildren<MeshRenderer>(true);var glow=renderers.First(r=>r.name=="TuningFork_Resonance");var levels=Enumerable.Range(0,3).Select(i=>new LOD(new[]{.27f,.10f,.003f}[i],new Renderer[]{renderers.First(r=>r.name=="TuningFork_LOD"+i),glow})).ToArray();var lod=go.GetComponent<LODGroup>();if(!lod)lod=go.AddComponent<LODGroup>();lod.SetLODs(levels);lod.RecalculateBounds();
                    var grip=go.AddComponent<CapsuleCollider>();grip.direction=1;grip.center=new Vector3(0,.1f,0);grip.height=1.2f;grip.radius=.085f;grip.isTrigger=true;
                    var head=go.AddComponent<BoxCollider>();head.center=new Vector3(0,1.05f,0);head.size=new Vector3(.68f,.94f,.24f);head.isTrigger=true;
                }
                PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+file+".prefab");
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        var presentation=new GameObject("Lord Lyrical finished show art");var art=presentation.AddComponent<ShowArtPresentation>();art.vortexPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Frequency_Vortex.prefab");art.filamentMaterial=energy;
        var prefab=PrefabUtility.SaveAsPrefabAsset(presentation,Root+"/Prefabs/ShowArtPresentation.prefab");UnityEngine.Object.DestroyImmediate(presentation);
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");config.finishingModel=prefab;config.orcWeapon=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Double_Headed_Tuning_Fork.prefab");
        string voicePath=Root+"/Voice/ShowVoiceSlots.asset";var voice=AssetDatabase.LoadAssetAtPath<VoiceCueLibrary>(voicePath);if(!voice){voice=ScriptableObject.CreateInstance<VoiceCueLibrary>();voice.entries=new[]{
            new VoiceCueLibrary.Entry{cueId="counter-call",actor="Jeru",slot=-1,recordingDirection="34 s. Key of A. Cantors to the front. Follow the line. Confirm final wording with Jeru; no recording supplied."},
            new VoiceCueLibrary.Entry{cueId="deep-sonar",actor="Seeker",slot=6,recordingDirection="83 s. Discover the dragon's original root key E. The Blade. Recording pending."},
            new VoiceCueLibrary.Entry{cueId="identify-discordant",actor="Jeru",slot=-1,recordingDirection="87 s. Address the Discordant: expose E beneath Bb. Recording pending."},
            new VoiceCueLibrary.Entry{cueId="amplitude-rise",actor="Jeru",slot=-1,recordingDirection="92 s. All ten. One frequency. Recording pending."}};AssetDatabase.CreateAsset(voice,voicePath);}config.voiceCues=voice;EditorUtility.SetDirty(config);
        // Change only the Orc shader implementation, retaining all authored textures and material values.
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/LordLyricalOrc_Unity/Materials"})){
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(m.shader.name=="LordLyrical/Sonic Orc Surface"){m.shader=Shader.Find("LordLyrical/Resonant Actor Surface");m.SetColor("_EdgeColor",new Color(2,.38f,.015f));EditorUtility.SetDirty(m);}
        }
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/LordLyrical/ScriptableObjects/MobileURP.asset");if(pipeline){var so=new SerializedObject(pipeline);so.FindProperty("m_MainLightRenderingMode").intValue=1;so.FindProperty("m_MainLightShadowsSupported").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);}
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");foreach(string name in new[]{"LordLyrical/Resonant Filament","LordLyrical/Resonant Actor Surface"}){var shader=Shader.Find(name);bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}}graphics.ApplyModifiedPropertiesWithoutUndo();
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Additive);
        try{SceneManager.SetActiveScene(scene);var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Chamber moon key");if(!existing){var light=new GameObject("Chamber moon key").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-30,0);light.color=new Color(.63f,.77f,1);light.intensity=1.3f;light.shadows=LightShadows.None;}RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);EditorSceneManager.SaveScene(scene);}finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
        AssetDatabase.SaveAssets();Debug.Log("FINISHING_ASSETS_CONNECTED");
    }
    [MenuItem("Lord Lyrical/Finishing/Validate and Render Complete Show")]
    public static void Validate()=>Review(false);
    [MenuItem("Lord Lyrical/Finishing/Render Complete Motion Preview")]
    public static void Movie()=>Review(true);
    [MenuItem("Lord Lyrical/Finishing/Update Revised Preview Segments")]
    public static void PatchMovie()=>Review(true,true);
    static void Review(bool movie,bool patch=false)
    {
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);var report=new StringBuilder("LORD LYRICAL FINISHING VALIDATION\n");
        try{
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");Require(config.finishingModel&&config.orcWeapon&&config.voiceCues,"Unassigned finishing assets");
            var session=new GameObject("Review session").AddComponent<NetworkSessionController>();var world=new GameObject("Complete show").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var art=world.GetComponentInChildren<ShowArtPresentation>();Require(art,"Missing art director");
            var camera=new GameObject("Sequence review camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2.2f,-7);camera.transform.rotation=Quaternion.Euler(-7,0,0);camera.fieldOfView=82;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.009f,.016f);camera.cullingMask=1<<28;
            var light=new GameObject("Review moon key").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-30,0);light.color=new Color(.63f,.77f,1);light.intensity=1.3f;light.cullingMask=1<<28;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);
            foreach(var g in scene.GetRootGameObjects())foreach(var tr in g.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
            Require(world.GetComponentsInChildren<ParticipantAvatarPerformance>(true).Length==10,"Ten avatars missing");Require(world.GetComponentsInChildren<SonicOrcPerformance>(true).Length==12,"Twelve Orcs missing");
            var shader=Shader.Find("LordLyrical/Resonant Actor Surface");Require(shader&&shader.isSupported,"Actor shader unsupported");Require(ShaderUtil.GetShaderMessages(shader).All(m=>m.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Actor shader compilation error");
            float[] times={4,20.6f,24,27,31.8f,34.8f,39.4f,43,45.5f,48,50.7f,53,59.5f,66,71,75,81.6f,85,90.6f,94,96.8f,98.6f,100,104,114,120,31.8f};
            var history=new Dictionary<float,string>();int peak=0;
            foreach(float t in times){
                world.Render(ShowFrame.At(t),true);foreach(var tr in world.GetComponentsInChildren<Transform>(true)){Vector3 p=tr.position;Require(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),"Nonfinite transform at "+t);tr.gameObject.layer=28;}
                peak=Math.Max(peak,art.ActiveRibbonCount);string signature=Fingerprint(world);if(history.TryGetValue(t,out string expected))Require(signature==expected,"Seek reconstruction differs at "+t);else history.Add(t,signature);
                if(!movie)ParticipantAvatarBuilder.Render(camera,Output+"/Show_"+t.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+".png",1600,900,1);
                int renderers=world.GetComponentsInChildren<Renderer>().Count(r=>r.enabled&&r.gameObject.activeInHierarchy);report.AppendLine($"PASS {t:F1}s; {art.ActiveRibbonCount} ribbons; {renderers} active renderer components (includes LOD alternatives before culling)");
            }
            world.ResetEffects();world.Render(ShowFrame.At(31.8),true);Require(Fingerprint(world)==history[31.8f],"Reset reconstruction mismatch");
            var chamber=world.GetComponentInChildren<ChamberPresentation>(true);world.Render(ShowFrame.At(99.5),true);var dragon=chamber.transform.Find("Current CyberDragon | chamber performance");Require(dragon&&Mathf.Abs(dragon.localScale.x-chamber.dragonScale)<.001f,"Dragon incorrectly shrinks");Require(dragon.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Any(m=>m.HasProperty("_Dissolve")&&m.GetFloat("_Dissolve")>.4f),"Dragon material dissolve missing");
            world.Render(ShowFrame.At(101),true);Require(!dragon.gameObject.activeSelf,"Dragon visible after dissolution");world.Render(ShowFrame.At(120),true);Require(art.TitleActive,"Final title missing");
            Require(config.voiceCues.entries.Length==4&&config.voiceCues.entries.All(e=>e.recording==null),"Expected four empty recording slots per user request");
            LordLyrical.Editor.ShowVerification.VerifyCore();report.AppendLine("PASS arbitrary seek and reset, constant dragon scale with real material dissolve, final title, 4 empty voice slots, core suite; peak ribbon count "+peak);
            if(movie){string frames=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\work\finishing\movie";Directory.CreateDirectory(frames);for(int f=0;f<121*12;f++){if(patch&&!((f>=34*12&&f<37*12)||(f>=59*12&&f<61*12)||(f>=69*12&&f<90*12)))continue;world.Render(ShowFrame.At(f/12d),true);ParticipantAvatarBuilder.Render(camera,frames+"/Show_"+f.ToString("D5")+".png",1280,720,1);}report.AppendLine(patch?"Revised 34-37s and 59-61s preview segments; full frame sequence retained.":"Rendered 1452 frames at 12fps across the full 121-second sequence.");}
            File.WriteAllText(Output+"/Unity_Validation.txt",report.ToString());Debug.Log("FINISHING_VALIDATION_PASS\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
    static string Fingerprint(ResonantWorld world)
    {var values=world.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.enabled).SelectMany(l=>Enumerable.Range(0,l.positionCount).Select(i=>l.GetPosition(i).ToString("F4")));return string.Join("|",values);}
    [MenuItem("Lord Lyrical/Finishing/Export Complete Art Package")]
    public static void Export(){AssetDatabase.SaveAssets();AssetDatabase.ExportPackage(new[]{Root,"Assets/LordLyricalChamber_Unity","Assets/LordLyricalJeru_Unity","Assets/LordLyricalAvatars_Unity","Assets/LordLyricalOrc_Unity","Assets/LordLyricalDragon_Unity","Assets/LordLyrical"},Path.GetDirectoryName(Output)+"/LordLyrical_Complete_Art.unitypackage",ExportPackageOptions.Recurse);Debug.Log("FINISHING_PACKAGE_EXPORTED");}
}
