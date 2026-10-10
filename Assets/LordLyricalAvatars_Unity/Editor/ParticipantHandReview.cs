using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical.Characters;
public static class ParticipantHandReview
{
 [MenuItem("Lord Lyrical/Participants/Render First Person Hands")]
 public static void Render(){
  var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try{
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ParticipantAvatarBuilder.Root+"/Prefabs/Cs_Seeker.prefab"));var p=root.GetComponent<ParticipantAvatarPerformance>();p.SampleClip(5,.35f);p.SetFirstPerson(true);p.SetResonance(.6f);
   var left=new GameObject("Left tracking target").transform;var right=new GameObject("Right tracking target").transform;left.position=new Vector3(-.22f,1.35f,.45f);right.position=new Vector3(.22f,1.35f,.45f);left.rotation=Quaternion.Euler(-25,-12,-15);right.rotation=Quaternion.Euler(-25,12,15);p.ApplyTrackedHands(left,right);
   foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
   var camera=new GameObject("Headset viewpoint").AddComponent<Camera>();camera.transform.position=new Vector3(0,1.55f,0);camera.transform.rotation=Quaternion.Euler(18,0,0);camera.fieldOfView=72;camera.nearClipPlane=.02f;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.032f,.05f);
   var light=new GameObject("Hand studio light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,145,0);light.intensity=1.6f;light.cullingMask=1<<30;
   ParticipantAvatarBuilder.Render(camera,ParticipantAvatarBuilder.Output+"/First_Person_Hands.png",1400,900,0);Debug.Log("FIRST_PERSON_HANDS_RENDERED");
  }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
 }
}
