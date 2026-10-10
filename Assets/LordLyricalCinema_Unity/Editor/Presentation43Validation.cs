using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class Presentation43Validation {
 static void Need(bool ok,string why){if(!ok)throw new Exception("PRESENTATION40: "+why);}
 public static void Run(){
  Directory.CreateDirectory(Production43Review.Out);
  EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity",OpenSceneMode.Single);
  Need(StoryScore40.Awakening(20)==0&&StoryScore40.Awakening(23)>0&&StoryScore40.Awakening(26)==0,"awakening cue drift");Need(StoryScore40.Reveal(62.9f)==0&&StoryScore40.Reveal(69)==0,"reveal cue drift");Need(StoryScore40.Discord(68)==0&&StoryScore40.Discord(81)==0,"dissonance cue drift");Need(StoryScore40.Quiet(97)<.25f&&StoryScore40.Quiet(98)==1,"Word contrast timing");Need(StoryScore40.Art(97)==0&&StoryScore40.Art(106)==1&&StoryScore40.Art(111)==0,"art hold timing");Need(StoryScore40.Farewell(118)==0&&StoryScore40.Farewell(121)>.99f&&StoryScore40.Farewell(124)==0,"gift/title overlap");Need(StoryScore40.Trace(24)==0,"landing trace leaks into VR");
  var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);PersonalFarewell40 gift=null;
  bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
  try{
   var session=new GameObject("Story40 review session").AddComponent<NetworkSessionController>();session.Participants.participants=Enumerable.Range(0,10).Select(i=>new ParticipantRecord{slot=i,clientNo=i,deviceId="fixture-"+i,connected=true}).ToArray();
   var world=new GameObject("Story40 world").AddComponent<ResonantWorld>();world.Initialize(AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset"),session);
   var cam=new GameObject("Story40 camera").AddComponent<Camera>();cam.transform.position=new Vector3(-1,1.7f,2);cam.transform.LookAt(new Vector3(0,5,12));cam.fieldOfView=85;cam.nearClipPlane=.02f;cam.farClipPlane=100;cam.cullingMask=1<<28;cam.backgroundColor=new Color(.025f,.028f,.04f);
   var light=new GameObject("Story40 light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,-25,0);light.cullingMask=1<<28;
   var story=world.GetComponent<StoryPresentation40>();int peak=0;
   foreach(float t in new[]{23f,30f,63.4f,65f,70f,80f,97.5f,98.1f,101.2f,106f,110.5f}){
    world.Render(ShowFrame.At(t),true);var e=world.GetComponent<ConcertInteraction>().Encounter;
    if(t==30){e.State.orcs[0].position=new Vector3(0,0,6);e.State.orcs[0].intent=OrcIntent.Telegraph;e.State.orcs[0].attackStarted=29.8f;e.State.orcs[1].health=0;e.State.orcs[1].defeatedAt=29.7f;}
    story.Render(t,true);peak=Math.Max(peak,story.ActiveParticles);Need(story.ActiveParticles<=StoryPresentation40.Budget&&story.ActiveLines<=StoryPresentation40.LineCount,"story pool overflow");Layer(scene);ParticipantAvatarBuilder.Render(cam,Production43Review.Out+"/Story_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1280,960,1);
   }
   session.Participants.participants=new[]{new ParticipantRecord{slot=0,connected=true}};story.Render(106,true);Need(Shader.GetGlobalInt("_ActiveRootMask40")==1,"unassigned keys glow");session.Participants.participants=Array.Empty<ParticipantRecord>();story.Render(106,true);Need(Shader.GetGlobalInt("_ActiveRootMask40")==0,"absent keys remain active");story.Render(0,false);Need(story.ActiveParticles==0&&story.ActiveLines==0&&Shader.GetGlobalFloat("_Discord40")==0,"reset retains story FX");
   gift=new PersonalFarewell40(new GameObject("Gift fixture").transform);Vector3 palm=new Vector3(0,1.2f,3),founder=new Vector3(0,0,5);gift.Render(121,true,true,palm,Vector3.forward,founder,RootKeys.Slots[0].color);Need(gift.Visible&&Vector3.Distance(gift.Position,palm+Vector3.forward*.10f)<.001f,"gift does not settle in palm");gift.Render(121,true,false,palm,Vector3.forward,founder,Color.cyan);Need(!gift.Visible,"gift sticks after tracking loss");gift.Render(124,true,true,palm,Vector3.forward,founder,Color.cyan);Need(!gift.Visible,"gift overlaps title");
   gift.Render(121,true,true,palm,Vector3.forward,founder,RootKeys.Slots[0].color);world.gameObject.SetActive(false);cam.transform.position=palm+new Vector3(.03f,.08f,-.25f);cam.transform.LookAt(gift.Position);Layer(scene);ParticipantAvatarBuilder.Render(cam,Production43Review.Out+"/Personal_Farewell_Detail.png",1200,900,1);
   gift.Render(0,false,false,palm,Vector3.forward,founder,Color.cyan);Need(!gift.Visible,"restart retains gift");
   var shader=Shader.Find("LordLyrical/Chamber Surface");Need(shader&&shader.isSupported&&!ShaderUtil.GetShaderMessages(shader).Any(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"chamber shader compilation");
   File.WriteAllText(Production43Review.Out+"/Presentation41_Validation.txt","PASS cue-bounded identity, reveal, dissonance, Word contrast, art hold, landing trace and farewell; ten/one/zero connected-key masks; bounded story geometry/particles; gift travel/palm settlement/tracking-loss hide/title separation/reset; chamber shader. Story particle peak: "+peak+" / "+StoryPresentation40.Budget+". Images are synthetic fixtures, not native headset acceptance.\n");Debug.Log("PRESENTATION40_PASS");
  }finally{gift?.Dispose();EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
 }
 static void Layer(Scene scene){foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;}
}
