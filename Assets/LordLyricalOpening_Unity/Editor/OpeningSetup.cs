using System;
using System.Linq;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Layers;
using UnityEngine.XR.OpenXR.Features.CompositionLayers;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using Unity.XR.OpenXR.Features.PICOSupport;
using LordLyrical;
using LordLyrical.Editor;
public static class OpeningSetup
{
    [MenuItem("Lord Lyrical/Opening/Configure Shared Arrival")]
    public static void Configure()
    {
        const string path="Assets/LordLyrical/Scenes/Experience.unity";
        if(EditorSceneManager.GetActiveScene().path!=path)throw new Exception("Open the authored Experience scene before configuring arrival.");
        var rig=UnityEngine.Object.FindFirstObjectByType<TrackedRig>();
        if(!rig)throw new Exception("Authored tracked rig missing.");
        Undo.RecordObject(rig.transform,"Move viewing mark closer to JERU");
        rig.transform.position=SharedRoomAlignment.VenueMark;
        var camera=rig.head.GetComponent<Camera>();Undo.RecordObject(camera,"Enable passthrough alpha");camera.clearFlags=CameraClearFlags.SolidColor;var color=camera.backgroundColor;color.a=0;camera.backgroundColor=color;camera.allowHDR=false;
        var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");cfg.countdownDuration=OpeningTiming.Prelude;EditorUtility.SetDirty(cfg);
        var shader=Shader.Find("LordLyrical/OpeningPortal");if(!shader)throw new Exception("Opening portal shader missing.");
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var list=graphics.FindProperty("m_AlwaysIncludedShaders");bool exists=false;
        for(int i=0;i<list.arraySize;i++)exists|=list.GetArrayElementAtIndex(i).objectReferenceValue==shader;
        if(!exists){int i=list.arraySize;list.InsertArrayElementAtIndex(i);list.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
        graphics.ApplyModifiedPropertiesWithoutUndo();
        ShowBuilder.ConfigureAndroid();
        var pipeline=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/LordLyrical/ScriptableObjects/MobileURP.asset"));
        pipeline.FindProperty("m_AllowPostProcessAlphaOutput").boolValue=false;pipeline.ApplyModifiedPropertiesWithoutUndo();
        var mobile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/LordLyrical/ScriptableObjects/MobileURP.asset");mobile.renderScale=.9f;EditorUtility.SetDirty(mobile);
        var feature=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android).GetFeature<PassthroughFeature>();
        if(!feature||!feature.enabled)throw new Exception("PICO passthrough feature was not enabled.");
        var composition=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android).GetFeature<OpenXRCompositionLayersFeature>();
        if(!composition||!composition.enabled)throw new Exception("OpenXR composition-layer support is missing.");
        var layerObject=rig.transform.Find("Passthrough projection layer");
        if(layerObject && layerObject.GetComponent<CompositionLayer>()){
            // Package editor emulation owns default layers; deactivate before removal.
            layerObject.gameObject.SetActive(false);UnityEngine.Object.DestroyImmediate(layerObject.gameObject);layerObject=null;
        }
        if(!layerObject){var go=new GameObject("Passthrough projection layer");go.transform.SetParent(rig.transform,false);layerObject=go.transform;}
        if(!layerObject.GetComponent<PassthroughProjectionLayer>())layerObject.gameObject.AddComponent<PassthroughProjectionLayer>();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("SHARED_ARRIVAL_CONFIGURED: 15 seconds room, 2 seconds arrival, song-relative cues preserved, 3 m viewing mark.");
    }
    [MenuItem("Lord Lyrical/Opening/Validate Composition Support")]
    public static void ValidateComposition()
    {
        var layers=UnityEngine.Object.FindObjectsByType<PassthroughProjectionLayer>(FindObjectsSortMode.None);
        if(layers.Length!=1)throw new Exception("Expected exactly one runtime source-alpha projection bootstrap.");
        var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if(!xr.GetFeature<OpenXRCompositionLayersFeature>().enabled||!xr.GetFeature<PassthroughFeature>().enabled)throw new Exception("Required passthrough or composition feature disabled.");
        var camera=UnityEngine.Object.FindFirstObjectByType<TrackedRig>().head.GetComponent<Camera>();
        if(camera.allowHDR||camera.backgroundColor.a>.001f||camera.clearFlags!=CameraClearFlags.SolidColor)throw new Exception("Initial camera must preserve transparent pixels.");
        string report="PASS: one runtime source-alpha projection bootstrap, composition and PICO passthrough features enabled, transparent SDR opening camera. Native room appearance still requires headset validation.";
        System.IO.File.WriteAllText(MotionPolishValidation.Out+"/Composition_Validation.txt",report);Debug.Log(report);
    }
}
