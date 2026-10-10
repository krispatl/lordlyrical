using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class Crescendo38Preview {
 public static void Run(){
  var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try{
   var session=new GameObject("Crescendo38 review").AddComponent<NetworkSessionController>();var world=new GameObject("Crescendo38 temple").AddComponent<ResonantWorld>();world.Initialize(AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset"),session);
   var cam=new GameObject("Crescendo38 camera").AddComponent<Camera>();cam.transform.position=new Vector3(-2,1.7f,2);cam.transform.LookAt(new Vector3(0,7,12));cam.fieldOfView=85;cam.nearClipPlane=.08f;cam.farClipPlane=100;cam.cullingMask=1<<28;cam.backgroundColor=new Color(.025f,.028f,.04f);
   var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,-25,0);light.cullingMask=1<<28;
   var e=new ConcertEncounter();var fx=new GameObject("Crescendo38 fixed pool").AddComponent<Crescendo38>();fx.Initialize(e);
   foreach(float t in new[]{91f,94f,97.5f,98f,98.4f,101f}){
    world.Render(ShowFrame.At(t),true);e.SetDuelTargets(Vector3.zero,world.GetComponent<ConcertInteraction>().Encounter.State.dragonHeart);e.State.finaleCharge=.85f;e.State.supportMask=1023;
    for(int i=0;i<10;i++){e.occupied[i]=true;e.heads[i]=new Vector3((i%5-2)*.8f,1.65f,2+(i/5)*1.2f);}
    fx.Render(t,true);if(fx.ActiveRibbons>Crescendo38.RibbonCount||t==91&&fx.ActiveRibbons!=0||t==101&&fx.ActiveRibbons!=0)throw new Exception("Crescendo pool/gating regression");
    foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
    ParticipantAvatarBuilder.Render(cam,Production38Review.Out+"/Crescendo_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
   }
  }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);Shader.SetGlobalFloat("_Crescendo38",0);}
 }
}
