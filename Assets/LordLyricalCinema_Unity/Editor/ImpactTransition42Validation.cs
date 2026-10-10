using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class ImpactTransition42Validation {
 static void Need(bool ok,string message){if(!ok)throw new Exception("IMPACT_TRANSITION41: "+message);}
 public static void Run(){
  Directory.CreateDirectory(Production42Review.Out);
  Need(Transition41.Entrance(8,false)==0&&Transition41.Entrance(8.5f,false)>.49f&&Transition41.Entrance(9,false)==1,"handover timing");
  Need(Transition41.Entrance(120,false)==1&&Transition41.Entrance(8.5f,true)==0,"wait/reset entrance");
  Need(CinemaTiming.ReturnStart==113.5f&&CinemaTiming.RealmGone==118.2f&&CinemaTiming.FounderGone==124&&CinemaTiming.EndTitleStart==124.25f,"exit cue drift");
  EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);
  var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
  try{
   var effect=new GameObject("Impact41 fixture").AddComponent<CombatImpactVFX>();effect.Initialize(null);
   Vector3 point=new Vector3(0,1.3f,5),normal=Vector3.back;
   var camera=new GameObject("Impact41 camera").AddComponent<Camera>();camera.transform.position=new Vector3(.35f,1.55f,3.8f);camera.transform.LookAt(point);camera.fieldOfView=55;camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.027f,.035f);
   effect.Hit(point,normal,RootKeys.Slots[2].color,true,false,true);
   Need(effect.ActiveParticles>0&&effect.FlashParticles==0&&effect.RingParticles==0,"continuous contact flashes like damage");effect.Clear();
   foreach(bool lethal in new[]{false,true}){
    effect.Hit(point,normal,RootKeys.Slots[2].color,true,lethal);Need(effect.FlashParticles==1&&effect.RingParticles==1,"confirmed impact layers absent");
    effect.SimulatePreview(.08f);Layer(scene);ParticipantAvatarBuilder.Render(camera,Production42Review.Out+(lethal?"/Impact_Lethal.png":"/Impact_Armour.png"),1024,768,1);effect.Clear();
   }
   effect.Hit(new Vector3(float.NaN,0,0),normal,Color.white,true);Need(effect.ActiveParticles==0,"nonfinite hit accepted");
   for(int i=0;i<100;i++)effect.Hit(point,normal,Color.cyan,true,true);
   Need(effect.ActiveParticles<=CombatImpactVFX.ParticleBudget,"impact pool overflow");effect.Clear();Need(effect.ActiveParticles==0,"reset retains impact");
   var session=new GameObject("Transition41 session").AddComponent<NetworkSessionController>();var world=new GameObject("Transition41 world").AddComponent<ResonantWorld>();var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");world.Initialize(config,session);
   camera.transform.position=new Vector3(0,1.7f,2);camera.transform.LookAt(new Vector3(0,5,12));camera.fieldOfView=85;
   var light=new GameObject("Transition41 light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,-25,0);light.cullingMask=1<<28;
   foreach(float t in new[]{8f,8.25f,8.5f,8.75f,9f,114.5f,116f,118.2f}){
    world.Render(ShowFrame.At(Mathf.Max(21,t)),true);Shader.SetGlobalFloat("_RealmEntrance41",Transition41.Entrance(t,false));Shader.SetGlobalFloat("_RealmDissolve",CinemaTiming.Realm(t));Layer(scene);ParticipantAvatarBuilder.Render(camera,Production42Review.Out+"/Transition_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
   }
   foreach(var shader in new[]{Shader.Find("LordLyrical/Chamber Surface"),Resources.Load<Shader>("Cinema26/RealmPeel"),Shader.Find("LordLyrical/OpeningPortal")})Need(shader&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"transition shader compile failure");
   File.WriteAllText(Production42Review.Out+"/ImpactTransition41_Validation.txt","PASS sustained sparks without flare/ring; confirmed damage flare+harmonic ring; lethal effect; invalid contact rejection; 100-hit pool stress <=992; clear/reset; entrance envelope8-9s and preserved return/title times; compiled transition shaders. Images are synthetic, not worn-headset or physical multi-headset acceptance.");
   Debug.Log("IMPACT_TRANSITION41_PASS");
  }finally{Shader.SetGlobalFloat("_RealmEntrance41",1);Shader.SetGlobalFloat("_RealmDissolve",0);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
 }
 static void Layer(Scene scene){foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;}
}
