using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Unity.XR.CoreUtils;
using Styly.NetSync;

namespace LordLyrical.Editor
{
    public static class ShowBuilder
    {
        const string Root="Assets/LordLyrical";
        [MenuItem("Lord Lyrical/Build Show Scene")]
        public static void BuildScene()
        {
            foreach(var p in new[]{"Scenes","Prefabs/Network","ScriptableObjects","Art/Materials","Art/Characters"})Directory.CreateDirectory(Root+"/"+p);
            AssetDatabase.Refresh();
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>(Root+"/ScriptableObjects/Experience.asset");
            if(config==null){config=ScriptableObject.CreateInstance<ExperienceConfiguration>();AssetDatabase.CreateAsset(config,Root+"/ScriptableObjects/Experience.asset");}
            config.track=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/LordLyrical.mp3");
            config.cueJson=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Resources/Cues.json");
            config.lordPortrait=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Characters/JeruPortrait.png");
            config.orcModel=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Art/Characters/SonicOrc.fbx");
            var importer=AssetImporter.GetAtPath(Root+"/Audio/LordLyrical.mp3") as AudioImporter;
            if(importer!=null){var s=importer.defaultSampleSettings;s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.PCM;s.preloadAudioData=true;importer.defaultSampleSettings=s;importer.SaveAndReimport();}
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
            ConfigureRendering();ConfigureAndroid();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>(Root+"/ScriptableObjects/Experience.asset");
            RenderSettings.ambientLight=new Color(.1f,.12f,.18f);RenderSettings.skybox=null;RenderSettings.fog=false;
            var rig=new GameObject("Physical XR Origin");var origin=rig.AddComponent<XROrigin>();rig.transform.position=new Vector3(0,0,-5);
            var trackingSpace=ResonantGeometry.Node("Floor tracking space",rig.transform,Vector3.zero);
            origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;
            var head=ResonantGeometry.Node("Tracked Head",trackingSpace,new Vector3(0,1.7f,0));
            var camera=head.gameObject.AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.004f,.006f,.015f);camera.nearClipPlane=.08f;camera.farClipPlane=90;camera.fieldOfView=78;
            head.gameObject.AddComponent<AudioListener>();origin.Camera=camera;origin.CameraFloorOffsetObject=trackingSpace.gameObject;
            var left=ResonantGeometry.Node("Left controller",trackingSpace,new Vector3(-.3f,1.25f,.3f));var right=ResonantGeometry.Node("Right controller",trackingSpace,new Vector3(.3f,1.25f,.3f));
            var tracking=rig.AddComponent<TrackedRig>();tracking.head=head;tracking.left=left;tracking.right=right;
            var local=AvatarPrefab(false);var remote=AvatarPrefab(true);
            var ng=new GameObject("STYLY NetSync Manager");var manager=ng.AddComponent<NetSyncManager>();
            var so=new SerializedObject(manager);so.FindProperty("_localAvatarPrefab").objectReferenceValue=local;so.FindProperty("_remoteAvatarPrefab").objectReferenceValue=remote;
            so.FindProperty("_roomId").stringValue=config.roomId;so.FindProperty("_serverAddress").stringValue=config.serverOverride;so.FindProperty("_serverDiscoveryPort").intValue=49999;so.FindProperty("_transformSendRate").floatValue=20;so.ApplyModifiedPropertiesWithoutUndo();
            var systems=new GameObject("Experience systems");var session=systems.AddComponent<NetworkSessionController>();session.configuration=config;
            var source=systems.AddComponent<AudioSource>();source.clip=config.track;source.playOnAwake=false;source.spatialBlend=0;source.priority=0;
            var master=systems.AddComponent<AudioMasterClock>();master.source=source;
            var world=new GameObject("The Resonant Realm").AddComponent<ResonantWorld>();
            var haptics=systems.AddComponent<HapticsDirector>();var director=systems.AddComponent<ExperienceDirector>();director.configuration=config;director.session=session;director.audioClock=master;director.world=world;director.haptics=haptics;
            var diagnostics=systems.AddComponent<OperatorDiagnostics>();diagnostics.director=director;diagnostics.session=session;
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/Experience.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/Experience.unity",true)};
            Debug.Log("LL BUILD SCENE PASS: Experience, PICO settings, authored cue table and NetSync prefabs saved");
        }
        static GameObject AvatarPrefab(bool remote)
        {
            var g=new GameObject(remote?"Remote Root Key Avatar":"Local Tracked Avatar");var a=g.AddComponent<NetSyncAvatar>();
            a._head=ResonantGeometry.Node("Head",g.transform,new Vector3(0,1.7f,0));a._leftHand=ResonantGeometry.Node("Left",g.transform,new Vector3(-.3f,1.25f,.3f));a._rightHand=ResonantGeometry.Node("Right",g.transform,new Vector3(.3f,1.25f,.3f));a._virtualTransforms=Array.Empty<Transform>();
            g.AddComponent<NetworkRoleAvatar>();
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/Network/"+(remote?"RemoteAvatar":"LocalAvatar")+".prefab");UnityEngine.Object.DestroyImmediate(g);return prefab;
        }
        static void ConfigureRendering()
        {
            var path=Root+"/ScriptableObjects/MobileURP.asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if(pipeline==null){var data=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(data,Root+"/ScriptableObjects/MobileRenderer.asset");pipeline=UniversalRenderPipelineAsset.Create(data);AssetDatabase.CreateAsset(pipeline,path);}
            pipeline.supportsHDR=false;pipeline.msaaSampleCount=4;pipeline.renderScale=1;pipeline.supportsCameraDepthTexture=false;pipeline.supportsCameraOpaqueTexture=false;
            var ps=new SerializedObject(pipeline);ps.FindProperty("m_MainLightRenderingMode").intValue=0;ps.FindProperty("m_AdditionalLightsRenderingMode").intValue=0;ps.FindProperty("m_MainLightShadowsSupported").boolValue=false;ps.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;ps.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;QualitySettings.vSyncCount=0;GraphicsSettings.useScriptableRenderPipelineBatching=true;EditorUtility.SetDirty(pipeline);
            // Custom shaders are generated into runtime geometry, so keep them in the build.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"LordLyrical/Resonance","LordLyrical/Wave","LordLyrical/ChamberSurfel"}){var shader=Shader.Find(name);bool exists=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;if(!exists){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}}graphics.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Lord Lyrical/Configure PICO Android")]
        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName="5th Yang LLC";PlayerSettings.productName="Lord Lyrical - The Resonant Realm";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.fifthyang.lordlyrical");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission=true;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.runInBackground=true;
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            var serialized=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);serialized.FindProperty("activeInputHandler").intValue=1;serialized.ApplyModifiedPropertiesWithoutUndo();
            var settings=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(Root+"/ScriptableObjects/XRSettings.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(settings,Root+"/ScriptableObjects/XRSettings.asset");}
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,settings,true);
            if(!settings.HasSettingsForBuildTarget(BuildTargetGroup.Android))settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if(!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var xr=settings.SettingsForBuildTarget(BuildTargetGroup.Android);xr.InitManagerOnStart=true;
            if(!XRPackageMetadataStore.AssignLoader(xr.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Android))throw new Exception("Could not assign OpenXR Android loader");
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var openxr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);openxr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
            var features=openxr.GetFeatures<OpenXRFeature>();bool pico=false;
            foreach(var f in features){string n=f.GetType().Name;bool enable=n=="PICOFeature"||n=="OpenXRExtensions"||n=="PICO4ControllerProfile"||n=="PICONeo3ControllerProfile"||n=="PICO4UltraControllerProfile"||n=="DisplayRefreshRateFeature"||n=="OpenXRCompositionLayersFeature"||n=="PassthroughFeature"||n=="FoveationFeature"||n=="HandTracking"||n=="PICOSpatialMesh"||n=="PICOSceneCapture";f.enabled=enable;if(n=="PICOFeature")pico=true;EditorUtility.SetDirty(f);}
            if(!pico)throw new Exception("PICO Support feature not discovered");
            var roomMesh=openxr.GetFeature<Unity.XR.OpenXR.Features.PICOSupport.PICOSpatialMesh>();
            if(roomMesh){roomMesh.LOD=Unity.XR.PXR.PxrMeshLod.Low;EditorUtility.SetDirty(roomMesh);}
            // The authored scene also runs in desktop XR. Unity's composition validation
            // checks the selected editor platform even when BuildPlayer targets Android.
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
            var desktop=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            var desktopComposition=desktop?.GetFeature<UnityEngine.XR.OpenXR.Features.CompositionLayers.OpenXRCompositionLayersFeature>();
            if(desktopComposition){desktopComposition.enabled=true;EditorUtility.SetDirty(desktopComposition);EditorUtility.SetDirty(desktop);}

            var picoConfig=Unity.XR.OpenXR.Features.PICOSupport.PICOProjectSetting.GetProjectConfig();picoConfig.isHandTracking=true;picoConfig.highFrequencyHand=true;picoConfig.handTrackingSupportType=Unity.XR.PXR.HandTrackingSupport.HandsOnly;picoConfig.displayFrequency=Unity.XR.OpenXR.Features.PICOSupport.SystemDisplayFrequency.RefreshRate72;picoConfig.foveationEnable=true;picoConfig.foveatedRenderingMode=FoveationFeature.FoveatedRenderingMode.FixedFoveatedRendering;picoConfig.foveatedRenderingLevel=FoveationFeature.FoveatedRenderingLevel.Medium;EditorUtility.SetDirty(picoConfig);
            var platform=Unity.XR.PXR.PXR_PlatformSetting.Instance;if(platform.appID==null)platform.appID=string.Empty;EditorUtility.SetDirty(platform);
            EditorUtility.SetDirty(settings);EditorUtility.SetDirty(xr);EditorUtility.SetDirty(xr.Manager);EditorUtility.SetDirty(openxr);AssetDatabase.SaveAssets();
        }
        [MenuItem("Lord Lyrical/Build PICO APK")]
        public static void BuildPico()=>BuildPicoVariant(false);
        [MenuItem("Lord Lyrical/Build PICO Diagnostic APK")]
        public static void BuildPicoDiagnostic()=>BuildPicoVariant(true);
        static void BuildPicoVariant(bool diagnostic)
        {
            // Persist the authored experience before Unity opens its build-time save prompt.
            // Unrelated open scenes retain their own save decisions.
            var experience = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Root+"/Scenes/Experience.unity");
            if (experience.IsValid() && experience.isLoaded && experience.isDirty && !EditorSceneManager.SaveScene(experience))
                throw new IOException("Could not save the authored Experience scene before building.");
            ConfigureAndroid();Directory.CreateDirectory("Builds");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Scenes/Experience.unity"},locationPathName="Builds/LordLyrical-PICO.apk",target=BuildTarget.Android,options=diagnostic?BuildOptions.Development:BuildOptions.None});
            File.WriteAllText("Builds/build-result.json",JsonUtility.ToJson(new BuildEvidence{development=diagnostic,result=report.summary.result.ToString(),errors=(int)report.summary.totalErrors,warnings=(int)report.summary.totalWarnings,bytes=(long)report.summary.totalSize},true));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("PICO build failed: "+report.summary.result);
        }
        [Serializable] class BuildEvidence{public bool development;public string result;public int errors,warnings;public long bytes;}
        [MenuItem("Lord Lyrical/Build Desktop Test Player")]
        public static void BuildDesktop()
        {PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);Directory.CreateDirectory("Builds/Desktop");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Scenes/Experience.unity"},locationPathName="Builds/Desktop/LordLyrical.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Desktop build failed");}
        public static void BuildDesktopAndVerify(){ConfigureAndroid();UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);ShowVerification.VerifyCore();ShowVerification.RenderFrames();BuildDesktop();}
    }
}

