using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class Presence43Validation {
 static void Need(bool ok,string message){if(!ok)throw new Exception("PRESENCE43: "+message);}
 public static void Run(){
  Directory.CreateDirectory(Production43Review.Out);EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);
  var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);PersonalFarewell40 fragment=null;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
  try{
   Vector3 eye=new Vector3(0,1.7f,2),front=Farewell42.Front(eye,Quaternion.identity),founder=new Vector3(0,0,5);
   Need(Vector3.Distance(front,eye)>.80f&&Vector3.Distance(front,eye)<.90f,"front-eye distance");
   fragment=new PersonalFarewell40(new GameObject("Farewell42 fixture").transform);
   fragment.RenderInteractive(121,true,eye,Quaternion.identity,default,default,founder,RootKeys.Slots[2].color,.014f);Need(fragment.Visible&&Vector3.Distance(fragment.Position,front)<.02f,"not visible in front without hands");
   var h=new HandPoseInput.HandState{tracked=true,closed=true,position=front+Vector3.right*.06f,direction=Vector3.back,rotation=Quaternion.identity};
   fragment.RenderInteractive(121.05f,true,eye,Quaternion.identity,h,default,founder,RootKeys.Slots[2].color,.014f);Need(fragment.Visible&&fragment.HandInfluence>.8f&&Vector3.Distance(fragment.Position,front)<.12f,"near-hand response not bounded");
   fragment.RenderInteractive(121.1f,true,eye,Quaternion.identity,default,default,founder,RootKeys.Slots[2].color,.014f);Need(fragment.Visible&&fragment.HandInfluence==0&&Vector3.Distance(fragment.Position,front)<.02f,"hand loss leaves fragment attached");
   h.position=front+Vector3.right*2;fragment.RenderInteractive(121.15f,true,eye,Quaternion.identity,h,default,founder,RootKeys.Slots[2].color,.014f);Need(fragment.HandInfluence==0,"distant hand attracts fragment");
   fragment.RenderInteractive(124,true,eye,Quaternion.identity,h,default,founder,Color.cyan,.014f);Need(!fragment.Visible,"fragment/title overlap");fragment.RenderInteractive(0,false,eye,Quaternion.identity,default,default,founder,Color.cyan,.014f);Need(!fragment.Visible,"reset retains gift");
   var camera=new GameObject("Presence43 camera").AddComponent<Camera>();camera.transform.position=eye;camera.transform.rotation=Quaternion.identity;camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.035f,.045f);
   fragment.RenderInteractive(121,true,eye,Quaternion.identity,default,default,founder,RootKeys.Slots[2].color,.014f);Layer(scene);ParticipantAvatarBuilder.Render(camera,Production43Review.Out+"/Farewell_InFront.png",1280,960,1);fragment.Dispose();fragment=null;
   var session=new GameObject("Presence43 session").AddComponent<NetworkSessionController>();var world=new GameObject("Presence43 world").AddComponent<ResonantWorld>();var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");world.Initialize(config,session);
   var key=new GameObject("Presence43 key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.1f;key.transform.rotation=Quaternion.Euler(35,-25,0);key.cullingMask=1<<28;camera.transform.LookAt(new Vector3(0,6,14));camera.fieldOfView=85;
   var magic=world.GetComponent<StageMagic42>();int peak=0;
   foreach(float t in new[]{63.8f,65f,67f,82.25f,86f,93f,97f,98.2f,100.8f}){
    world.Render(ShowFrame.At(t),true);Need(magic.ActiveParticles<=StageMagic42.Budget&&magic.ActiveLines<=StageMagic42.LineCount&&magic.ActiveLights<=StageMagic42.LightCount,"presentation budget overflow");peak=Math.Max(peak,magic.ActiveParticles);Layer(scene);ParticipantAvatarBuilder.Render(camera,Production43Review.Out+"/DragonMagic_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
   }
   world.Render(ShowFrame.At(5),false);Need(magic.ActiveParticles==0&&magic.ActiveLines==0&&magic.ActiveLights==0,"hidden stage retains effects");
   Need(StageMagic42.DragonPower(62)==0&&StageMagic42.DragonPower(101)==0&&StageMagic42.Charge(82.25f)>0,"magic cue boundaries");
   foreach(string name in new[]{"LordLyrical/Chamber Surface","LordLyrical/Resonant Actor Surface","LordLyrical/Fist lattice shield","LordLyrical/Magical Tracked Hand"}){var shader=Shader.Find(name);Need(shader&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"shader error "+name);}
   File.WriteAllText(Production43Review.Out+"/Presence43_Validation.txt","PASS fragment front85cm without hands; bounded nearby-hand response/closed squeeze; far hand no attraction; tracking loss returns to float; title separation/reset; magic clock boundaries; pools320/10lines/4lights; stage hide; compiled light/magic/hand/shield shaders. Particle peak="+peak+". Native hand interaction, GPU frame timing and physical multi-headset/audience acceptance remain pending.");Debug.Log("PRESENCE43_PASS");
  }finally{fragment?.Dispose();EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
 }
 static void Layer(Scene s){foreach(var root in s.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;}
}
