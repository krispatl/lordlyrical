using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;
using Unity.XR.OpenXR.Features.PICOSupport;
using MRGraffiti;
public static class GraffitiBuilder {
 const string Root="Assets/MRGraffiti";
 public const string Out="C:/Users/kris/Documents/Codex/2026-10-03/bui/outputs/MRGraffiti/V2";
 public static void Build(){
  Directory.CreateDirectory(Out);Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);RenderSettings.skybox=null;RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
  Shader solid=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/CanMetal.shader");
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/ChromaWallCan.fbx");if(!model)throw new Exception("Missing authored can");
  var can=UnityEngine.Object.Instantiate(model);can.name="ChromaWallCan";
  foreach(var renderer in can.GetComponentsInChildren<Renderer>()){
   var prior=renderer.sharedMaterials;for(int i=0;i<prior.Length;i++){
    string name=prior[i]?prior[i].name:"Metal";string path=Root+"/Materials/"+name.Replace('/','_')+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!material){material=new Material(solid);Color color=prior[i]&&prior[i].HasProperty("_Color")?prior[i].color:new Color(.4f,.5f,.6f);color=name.Contains("Graphite")?new Color(.018f,.025f,.04f):name.Contains("lettering")?new Color(.85f,.91f,.94f):name.Contains("indicator")?new Color(.015f,.65f,.88f):name.Contains("polymer")?new Color(.035f,.045f,.065f):new Color(.55f,.62f,.68f);material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",name.Contains("aluminium")?.9f:name.Contains("Graphite")?.2f:.35f);AssetDatabase.CreateAsset(material,path);}prior[i]=material;
   }renderer.sharedMaterials=prior;
  }
  var nozzle=new GameObject("NozzleOrigin");nozzle.transform.SetParent(can.transform,false);nozzle.transform.localPosition=new Vector3(0,.13f,.025f);
  foreach(var tr in can.GetComponentsInChildren<Transform>(true))if(tr.name=="Cap0"||tr.name=="Cap2")tr.gameObject.SetActive(false);
  var prefab=PrefabUtility.SaveAsPrefabAsset(can,Root+"/Prefabs/ChromaWallCan.prefab");UnityEngine.Object.DestroyImmediate(can);
  var rig=new GameObject("Graffiti XR Origin");var origin=rig.AddComponent<XROrigin>();var space=new GameObject("Floor tracking space").transform;space.SetParent(rig.transform,false);
  var head=new GameObject("Tracked Head");head.transform.SetParent(space,false);head.transform.localPosition=Vector3.up*1.65f;var camera=head.AddComponent<Camera>();camera.tag="MainCamera";camera.nearClipPlane=.025f;camera.farClipPlane=35;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;head.AddComponent<AudioListener>();
  origin.Camera=camera;origin.CameraFloorOffsetObject=space.gameObject;origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;
  var left=new GameObject("Left controller").transform;left.SetParent(space,false);var right=new GameObject("Right controller").transform;right.SetParent(space,false);
  var app=new GameObject("Graffiti experience").AddComponent<GraffitiExperience>();app.view=camera;app.origin=space;app.left=left;app.right=right;app.canPrefab=prefab;app.wallShader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/WallPaint.shader");app.stampShader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/SprayStamp.shader");app.solidShader=solid;app.dripShader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/DripHead.shader");app.aerosolShader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/Aerosol.shader");
  EditorSceneManager.SaveScene(scene,Root+"/Scenes/MRGraffiti_Main.unity");AssetDatabase.SaveAssets();GraffitiValidation.Run(prefab,app.wallShader,app.stampShader,solid);
  var pico=PICOProjectSetting.GetProjectConfig();string picoPath=AssetDatabase.GetAssetPath(pico);byte[] picoBytes=File.ReadAllBytes(picoPath);
  var id=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);var nameBefore=PlayerSettings.productName;var version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
  try{
   pico.isHandTracking=false;pico.handTrackingSupportType=Unity.XR.PXR.HandTrackingSupport.ControllersAndHands;EditorUtility.SetDirty(pico);
   PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.fifthyang.chromawall");PlayerSettings.productName="Chroma Wall MR";PlayerSettings.bundleVersion="0.2.0";PlayerSettings.Android.bundleVersionCode=2;AssetDatabase.SaveAssets();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Scenes/MRGraffiti_Main.unity"},locationPathName=Out+"/ChromaWall-MR-PICO.apk",target=BuildTarget.Android,options=BuildOptions.None});
   File.WriteAllText(Out+"/Build.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+" bytes="+report.summary.totalSize);
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Graffiti Android build failed");
  }finally{
   PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,id);PlayerSettings.productName=nameBefore;PlayerSettings.bundleVersion=version;PlayerSettings.Android.bundleVersionCode=code;AssetDatabase.SaveAssets();File.WriteAllBytes(picoPath,picoBytes);AssetDatabase.ImportAsset(picoPath,ImportAssetOptions.ForceUpdate);
   EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);
  }
 }
}
