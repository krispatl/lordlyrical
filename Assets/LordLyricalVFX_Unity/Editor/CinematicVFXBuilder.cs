using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LordLyrical;

public static class CinematicVFXBuilder
{
    const string Root="Assets/LordLyricalVFX_Unity";
    public const string Output=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\LordLyrical_VFX";
    static void Need(bool pass,string message){if(!pass)throw new Exception("VFX: "+message);}
    static Material Mat(string name,string shader){string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}m.shader=Shader.Find(shader);return m;}
    static void Particle(string name,string map,float energy,bool smoke=false,bool sheet=false){var m=Mat(name,"LordLyrical/Cinematic Particles");m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+map+".png"));m.SetFloat("_Energy",energy);m.SetFloat("_Flipbook",sheet?1:0);m.SetFloat("_DstBlend",smoke?10:1);m.SetFloat("_SoftDistance",smoke?.55f:.22f);EditorUtility.SetDirty(m);}
    [MenuItem("Lord Lyrical/VFX/Build Cinematic Effects")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Profiles");AssetDatabase.Refresh();
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Textures"})){
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=importer.assetPath.Contains("Flow")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6,compressionQuality=80});importer.SaveAndReimport();
        }
        Particle("Sparks","Spark_Core",7);Particle("Plumes","Volumetric_Plume_8x8",1.15f,true,true);Particle("Energy","Volumetric_Plume_8x8",3.5f,false,true);Particle("Corona","Optical_Flare",6);
        var noise=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Energy_Flow_RGBA.png");var wave=Mat("Wavefront","LordLyrical/Cinematic Wavefront");wave.SetTexture("_Noise",noise);EditorUtility.SetDirty(wave);
        var vortex=Mat("Vortex_Curtains","LordLyrical/Cinematic Vortex");vortex.SetTexture("_Noise",noise);EditorUtility.SetDirty(vortex);var ward=Mat("Healer_Membrane","LordLyrical/Cinematic Resonance Shield");ward.SetTexture("_Noise",noise);EditorUtility.SetDirty(ward);
        var debris=Mat("Obsidian_Fragments","Universal Render Pipeline/Lit");debris.SetColor("_BaseColor",new Color(.16f,.22f,.29f));debris.SetFloat("_Metallic",.6f);debris.SetFloat("_Smoothness",.55f);debris.EnableKeyword("_EMISSION");debris.SetColor("_EmissionColor",new Color(.025f,.055f,.09f));EditorUtility.SetDirty(debris);
        var filament=AssetDatabase.LoadAssetAtPath<Material>("Assets/LordLyricalFinishing_Unity/Materials/Resonant_Filament.mat");filament.SetTexture("_Flow",noise);EditorUtility.SetDirty(filament);
        string pp=Root+"/Profiles/Cinematic_Resonance.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(pp);if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,pp);}
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(.36f);bloom.threshold.Override(.95f);bloom.scatter.Override(.64f);bloom.highQualityFiltering.Override(false);bloom.maxIterations.Override(6);bloom.tint.Override(new Color(.90f,.96f,1));
        if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
        if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(.15f);color.contrast.Override(8);color.saturation.Override(5);EditorUtility.SetDirty(profile);EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(tone);EditorUtility.SetDirty(color);
        string prefabPath="Assets/LordLyricalFinishing_Unity/Prefabs/ShowArtPresentation.prefab";var go=PrefabUtility.LoadPrefabContents(prefabPath);
        try{var p=go.GetComponent<CinematicParticleDirector>();if(!p)p=go.AddComponent<CinematicParticleDirector>();p.sparksMaterial=Mat("Sparks","LordLyrical/Cinematic Particles");p.plumeMaterial=Mat("Plumes","LordLyrical/Cinematic Particles");p.energyMaterial=Mat("Energy","LordLyrical/Cinematic Particles");p.flareMaterial=Mat("Corona","LordLyrical/Cinematic Particles");p.debrisMaterial=debris;p.waveMaterial=wave;p.shieldMaterial=ward;p.vortexMaterial=vortex;var look=go.GetComponent<CinematicLook>();if(!look)look=go.AddComponent<CinematicLook>();look.profile=profile;var volume=go.GetComponent<Volume>();if(volume)volume.sharedProfile=profile;PrefabUtility.SaveAsPrefabAsset(go,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(go);}
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/LordLyrical/ScriptableObjects/MobileURP.asset");var so=new SerializedObject(pipeline);so.FindProperty("m_SupportsHDR").boolValue=true;so.FindProperty("m_RequireDepthTexture").boolValue=true;so.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=3;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/LordLyrical/ScriptableObjects/MobileRenderer.asset");renderer.copyDepthMode=CopyDepthMode.AfterOpaques;renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");Need(renderer.postProcessData,"Post-process resources missing");EditorUtility.SetDirty(renderer);
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");foreach(string name in new[]{"LordLyrical/Cinematic Particles","LordLyrical/Cinematic Wavefront","LordLyrical/Cinematic Resonance Shield","LordLyrical/Cinematic Vortex"}){var shader=Shader.Find(name);Need(shader,name+" absent");bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}}graphics.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();Debug.Log("CINEMATIC_VFX_CONNECTED");
    }
    [MenuItem("Lord Lyrical/VFX/Remove Duplicate Avatar LOD Ownership")]
    public static void FixLods()
    {
        int fixedGroups=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/LordLyricalAvatars_Unity/Prefabs"})){
            string path=AssetDatabase.GUIDToAssetPath(guid);var go=PrefabUtility.LoadPrefabContents(path);
            try{var p=go.GetComponent<LordLyrical.Characters.ParticipantAvatarPerformance>();if(!p)continue;var owned=new HashSet<Renderer>();foreach(var group in new[]{p.bodyLod,p.handLod})if(group)foreach(var lod in group.GetLODs())foreach(var renderer in lod.renderers)owned.Add(renderer);
                foreach(var group in go.GetComponentsInChildren<LODGroup>(true))if(group!=p.bodyLod&&group!=p.handLod&&group.GetLODs().Any(l=>l.renderers.Any(r=>owned.Contains(r)))){group.enabled=false;group.SetLODs(Array.Empty<LOD>());fixedGroups++;}
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Output+"/LOD_Ownership_Repair.txt","Removed duplicate renderer ownership from "+fixedGroups+" imported nested LOD groups. The authored body and hand LOD groups retain control.\n");Debug.Log("AVATAR_LOD_OWNERSHIP_FIXED "+fixedGroups);
    }
    [MenuItem("Lord Lyrical/VFX/Validate and Render Hero Beats")]
    public static void Review()=>Capture(false);
    [MenuItem("Lord Lyrical/VFX/Render Motion Reel")]
    public static void Movie()=>Capture(true);
    static void Capture(bool movie)
    {
        Directory.CreateDirectory(Output);var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);var report=new StringBuilder("CINEMATIC VFX VALIDATION\n");
        try{
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");var session=new GameObject("VFX review session").AddComponent<NetworkSessionController>();var world=new GameObject("VFX full cast").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var fx=world.GetComponentInChildren<CinematicParticleDirector>();Need(fx,"Cinematic director missing");
            var camera=new GameObject("VFX review camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2.3f,-6.5f);camera.transform.rotation=Quaternion.Euler(-10,0,0);camera.fieldOfView=82;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.009f,.016f);camera.cullingMask=1<<28;CinematicLook.Configure(camera);
            var light=new GameObject("VFX review moon key").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-30,0);light.color=new Color(.63f,.77f,1);light.intensity=1.3f;light.cullingMask=1<<28;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);
            foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
            foreach(string name in new[]{"LordLyrical/Cinematic Particles","LordLyrical/Cinematic Wavefront","LordLyrical/Cinematic Resonance Shield","LordLyrical/Resonant Filament"}){var shader=Shader.Find(name);Need(shader&&shader.isSupported,"Unsupported "+name);Need(ShaderUtil.GetShaderMessages(shader).All(m=>m.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader errors "+name);}
            var times=new[]{5f,20.9f,21.2f,26.35f,31.8f,34.6f,38.8f,39.25f,39.8f,42.35f,45.8f,47.25f,48f,50.7f,53f,59.5f,63.5f,66.6f,68.7f,70.4f,75.5f,78f,81.4f,84.6f,90.3f,94f,97.4f,98.4f,99.5f,102f,105f,117f};
            var hashes=new Dictionary<float,ulong>();int peak=0;foreach(float t in times){world.Render(ShowFrame.At(t),true);Need(fx.AllFinite(),"Invalid particle transform "+t);Need(fx.DroppedParticles==0,"Particle pool overflow "+t);int native=fx.GetComponentsInChildren<ParticleSystem>().Sum(p=>p.particleCount);Need(native==fx.ActiveParticles,"Native particle count mismatch "+t+" actual="+native);hashes[t]=fx.Fingerprint();peak=Math.Max(peak,fx.ActiveParticles);if(!movie)Render(camera,Output+"/VFX_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1600,900);report.AppendLine($"PASS {t:F2}s | {fx.ActiveParticles} particles, {fx.ActiveLayers} systems, {fx.ActiveWavefronts} wavefronts");}
            foreach(float t in times.Reverse()){world.Render(ShowFrame.At(t),true);Need(fx.Fingerprint()==hashes[t],"Reverse seek differs "+t);}world.ResetEffects();Need(fx.ActiveParticles==0,"Reset did not clear particles");world.Render(ShowFrame.At(75.5),true);Need(fx.Fingerprint()==hashes[75.5f],"Reset reconstruction differs");
            int original=config.vfxQuality;try{config.vfxQuality=0;world.Render(ShowFrame.At(98.4),true);int low=fx.ActiveParticles;config.vfxQuality=2;world.Render(ShowFrame.At(98.4),true);Need(fx.ActiveParticles>low,"Quality settings do not change particle budget");Need(fx.DroppedParticles==0,"High profile pool overflow");report.AppendLine($"PASS quality density: low={low}; high={fx.ActiveParticles}");}finally{config.vfxQuality=original;}
            if(movie){string frames=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\work\vfx\movie";Directory.CreateDirectory(frames);for(int f=0;f<121*12;f++){world.Render(ShowFrame.At(f/12d),true);Render(camera,frames+"/VFX_"+f.ToString("D5")+".png",1280,720);}report.AppendLine("PASS rendered 1452 frames / 121 seconds / 12 source fps");}
            int sweepPeak=0;for(int f=0;f<121*12;f++){world.Render(ShowFrame.At(f/12d),true);Need(fx.AllFinite()&&fx.DroppedParticles==0,"Dense sweep failure "+f);sweepPeak=Math.Max(sweepPeak,fx.ActiveParticles);}
            LordLyrical.Editor.ShowVerification.VerifyCore();report.AppendLine("PASS full 1452-step particle sweep; peak="+sweepPeak+"; all nine core cue/clock/reconstruction groups");
            report.AppendLine("PASS deterministic reverse seek and reset; bounded particle pools; compiled XR shaders; peak normal-profile checkpoint particles="+peak);File.WriteAllText(Output+"/Unity_VFX_Validation.txt",report.ToString());Debug.Log("CINEMATIC_VFX_VALIDATED\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
    // The established character snapshot renderer also preserves native ParticleSystemRenderers.
    static void Render(Camera camera,string path,int width,int height){foreach(var root in camera.gameObject.scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;ParticipantAvatarBuilder.Render(camera,path,width,height,1);}
    [MenuItem("Lord Lyrical/VFX/Export Cinematic VFX Package")]
    public static void Export(){AssetDatabase.SaveAssets();AssetDatabase.ExportPackage(new[]{Root,"Assets/LordLyricalFinishing_Unity","Assets/LordLyrical"},Output+"/LordLyrical_Cinematic_VFX.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);Debug.Log("CINEMATIC_VFX_EXPORTED");}
}

