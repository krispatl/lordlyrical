using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering.Universal;
using Unity.XR.OpenXR.Features.PICOSupport;
namespace MRGraffiti {
 public sealed class GraffitiExperience:MonoBehaviour {
  public Camera view;public Transform origin,left,right;public GameObject canPrefab;public Shader wallShader,stampShader,solidShader,aerosolShader,dripShader;
  public Vector3 gripOffset=new Vector3(0,-.025f,.025f),gripEuler=new Vector3(-30,0,0);
  RoomWalls room;PaintEngine paint;GraffitiPalette palette;GameObject can;Transform nozzle;Renderer band;Transform[] caps=new Transform[3];
  readonly Cap[] definitions=Cap.Defaults;int color=8,cap=1,finish;static readonly string[] FinishNames={"MATTE","CHROME","HOLO"};bool suppressSpray;bool leftDominant,triggerWas,menuWas,gripWas,focused=true,sound=true,ready;
  InputDevice dominant;Vector3 lastPosition,lastVelocity;float statsAt;int statsFrames;float nextHaptic,nextParticle,nextShake,lastPoseAt;int shakeTurns;bool priorAcceleration;
  AudioSource sprayAudio,shakeAudio;AudioClip sprayClip,shakeClip;ParticleSystem aerosol;Material sprayMaterial,cursorMaterial;LineRenderer rayLine;GameObject cursor;TextMesh status;
  public int WallCount=>room?room.walls.Count:0;
  IEnumerator Start(){
   view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.clear;view.allowHDR=false;view.GetUniversalAdditionalCameraData().renderPostProcessing=false;
   paint=new PaintEngine(stampShader,dripShader);room=gameObject.AddComponent<RoomWalls>();room.paintShader=wallShader;room.trackingSpace=origin;
   can=Instantiate(canPrefab,right,false);can.name="CHROMA WALL | held aerosol";can.transform.localPosition=gripOffset;can.transform.localRotation=Quaternion.Euler(gripEuler);
   foreach(var tr in can.GetComponentsInChildren<Transform>(true)){if(tr.name=="NozzleOrigin")nozzle=tr;if(tr.name=="ColourBand")band=tr.GetComponent<Renderer>();for(int i=0;i<3;i++)if(tr.name=="Cap"+i)caps[i]=tr;}
   if(!nozzle){var g=new GameObject("NozzleOrigin");g.transform.SetParent(can.transform,false);g.transform.localPosition=new Vector3(0,.13f,.025f);nozzle=g.transform;}
   palette=new GraffitiPalette(solidShader,Select);SetCan();CreateFeedback();ready=true;
   var systems=new List<XRInputSubsystem>();
   for(int i=0;i<40;i++){SubsystemManager.GetSubsystems(systems);if(systems.Count>0){foreach(var s in systems)s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);break;}yield return new WaitForSecondsRealtime(.1f);}
   for(int i=0;i<50;i++){if(PassthroughFeature.isExtensionEnable){EnableRoom();break;}yield return new WaitForSecondsRealtime(.1f);}
   room.Query(false);Debug.Log("GRAFFITI_READY controllers; surface-only paint; max 4 walls; 6 undo snapshots");
  }
  void EnableRoom(){
#if UNITY_ANDROID && !UNITY_EDITOR
   if(PassthroughFeature.isExtensionEnable&&PassthroughFeature.IsPassthroughSupported()){PassthroughFeature.EnableVideoSeeThrough=true;PassthroughFeature.PassthroughStart();PassthroughFeature.SetPassthroughStyle(new PassthroughStyle{TextureOpacityFactor=1,enableColorMap=false,enableEdgeColor=false});}
#endif
  }
  void OnEnable()=>Application.onBeforeRender+=Track;
  void OnDisable(){Application.onBeforeRender-=Track;paint?.End();if(sprayAudio)sprayAudio.Stop();}
  void Track(){Apply(XRNode.Head,view?view.transform:null);Apply(XRNode.LeftHand,left);Apply(XRNode.RightHand,right);if(ready)palette.Follow(focused&&Tracked(InputDevices.GetDeviceAtXRNode(leftDominant?XRNode.RightHand:XRNode.LeftHand)));}
  static bool Apply(XRNode node,Transform target){if(!target)return false;var d=InputDevices.GetDeviceAtXRNode(node);if(!d.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked)||!tracked)return false;if(d.TryGetFeatureValue(CommonUsages.devicePosition,out var p))target.localPosition=p;if(d.TryGetFeatureValue(CommonUsages.deviceRotation,out var q))target.localRotation=q;return true;}
  static bool Tracked(InputDevice d)=>d.TryGetFeatureValue(CommonUsages.isTracked,out bool t)&&t;
  void Update(){
   if(!ready)return;Track();dominant=InputDevices.GetDeviceAtXRNode(leftDominant?XRNode.LeftHand:XRNode.RightHand);var other=InputDevices.GetDeviceAtXRNode(leftDominant?XRNode.RightHand:XRNode.LeftHand);
   dominant.TryGetFeatureValue(CommonUsages.trigger,out float pressure);dominant.TryGetFeatureValue(CommonUsages.gripButton,out bool grip);other.TryGetFeatureValue(CommonUsages.primaryButton,out bool menu);other.TryGetFeatureValue(CommonUsages.menuButton,out bool menuButton);menu|=menuButton;
   bool tracked=Tracked(dominant)&&focused;can.SetActive(tracked);if(menu&&!menuWas&&focused&&Tracked(other))palette.Toggle(leftDominant?right:left,view);menuWas=menu;
   if(pressure<.06f)suppressSpray=false;if(palette.Open)suppressSpray=true;
   bool trigger=pressure>.12f;bool click=trigger&&!triggerWas;
   var ray=new Ray(nozzle.position,nozzle.forward);Vector3 velocity=Vector3.zero;
   if(tracked&&lastPoseAt>0&&Time.unscaledTime-lastPoseAt<.15f)velocity=(nozzle.position-lastPosition)/Mathf.Max(.001f,Time.unscaledDeltaTime);
   if(tracked){lastPosition=nozzle.position;lastPoseAt=Time.unscaledTime;}else lastPoseAt=0;
   if(room.Mapping&&grip&&!gripWas&&tracked){room.Mark(nozzle.position);Haptic(.18f);}
   if(palette.Open){paint.End();if(tracked&&palette.Ray(ray,click)&&click)Haptic(.12f);rayLine.enabled=tracked&&Tracked(other)&&palette.Open;rayLine.SetPosition(0,ray.origin);rayLine.SetPosition(1,ray.GetPoint(Mathf.Min(1.2f,palette.PointerDistance)));}
   else rayLine.enabled=false;
   bool hit=room.Hit(ray,definitions[cap].range,out var wall,out var uv,out var point,out float distance,out float angle);
   cursor.SetActive(tracked&&hit&&!palette.Open&&!room.Mapping);
   if(hit){cursor.transform.position=point-ray.direction*.004f;cursor.transform.rotation=Quaternion.LookRotation(ray.direction);float radius=SprayModel.Radius(definitions[cap],distance);cursor.transform.localScale=Vector3.one*Mathf.Clamp(radius*.4f,.003f,.014f);}
   bool spraying=tracked&&!suppressSpray&&!palette.Open&&!room.Mapping&&pressure>.06f;
   if(spraying&&hit)paint.Spray(wall,uv,ray.direction,distance,angle,pressure,velocity.magnitude,Time.deltaTime,definitions[cap],GraffitiPalette.Colors[color],finish);else if(!spraying||!hit)paint.End();
   if(!trigger&&triggerWas)paint.End();
   if(focused)paint.Tick(Mathf.Min(Time.deltaTime,.05f));
   float target=spraying&&sound?Mathf.Pow(pressure,1.3f)*.20f:0;sprayAudio.volume=Mathf.MoveTowards(sprayAudio.volume,target,Time.unscaledDeltaTime*1.5f);sprayAudio.pitch=.8f+pressure*.4f;if(!sprayAudio.isPlaying&&target>0)sprayAudio.Play();
   if(spraying&&Time.unscaledTime>nextHaptic){Haptic(.025f+pressure*.065f);nextHaptic=Time.unscaledTime+.09f;}
   if(spraying&&Time.unscaledTime>nextParticle){nextParticle=Time.unscaledTime+.012f;float plumeDistance=hit?Mathf.Clamp(distance,.06f,.5f):.25f;float spread=SprayModel.Radius(definitions[cap],plumeDistance)/plumeDistance*.5f;Vector3 direction=(ray.direction+nozzle.right*Random.Range(-spread,spread)+nozzle.up*Random.Range(-spread,spread)).normalized;var emit=new ParticleSystem.EmitParams{position=nozzle.position,velocity=direction*2.2f,startColor=new Color(GraffitiPalette.Colors[color].r,GraffitiPalette.Colors[color].g,GraffitiPalette.Colors[color].b,.07f*pressure),startSize=.004f+.012f*pressure,startLifetime=hit?Mathf.Min(.25f,distance/2.2f):.12f};aerosol.Emit(emit,2);}
   Vector3 acceleration=(velocity-lastVelocity)/Mathf.Max(.005f,Time.deltaTime);bool positive=Vector3.Dot(acceleration,nozzle.up)>0;
   if(tracked&&!spraying&&acceleration.magnitude>9&&velocity.magnitude>.35f&&positive!=priorAcceleration){shakeTurns++;priorAcceleration=positive;if(shakeTurns>=3&&Time.unscaledTime>nextShake){if(sound)shakeAudio.PlayOneShot(shakeClip,.25f);Haptic(.08f);nextShake=Time.unscaledTime+.4f;shakeTurns=0;}}
   if(velocity.magnitude<.1f)shakeTurns=0;lastVelocity=velocity;
   status.text=!tracked?"Wake your spray controller":room.Mapping?room.Status:room.walls.Count==0?room.Status+"\nOther controller A/X: palette":palette.Open?"Other hand: colour / caps / finish / tools":definitions[cap].name+"  /  "+FinishNames[finish]+"\nA/X on other hand: palette";
   status.transform.position=view.transform.position+view.transform.forward*.85f-view.transform.up*.28f;status.transform.rotation=view.transform.rotation;
   statsFrames++;if(Time.unscaledTime-statsAt>10){Debug.Log("GRAFFITI_STATS focused="+focused+" controller="+tracked+" walls="+room.walls.Count+" stamps="+paint.StampCount+" undo="+paint.UndoCount+" fps="+(statsFrames/(Time.unscaledTime-statsAt)).ToString("F1")+" room="+room.Status);statsAt=Time.unscaledTime;statsFrames=0;}
   triggerWas=trigger;gripWas=grip;
  }
  void Select(int id){paint.End();if(id<16)color=id;else if(id>=20&&id<23)cap=id-20;else if(id>=30&&id<33)finish=id-30;else if(id==40)paint.Undo();else if(id==41)paint.Clear(room.walls);else if(id==42){room.BeginManual();palette.Hide();}else if(id==43){room.Query(true);palette.Hide();}else if(id==44){leftDominant=!leftDominant;can.transform.SetParent(leftDominant?left:right,false);can.transform.localPosition=gripOffset;can.transform.localRotation=Quaternion.Euler(gripEuler);palette.Hide();lastPoseAt=0;}else if(id==45)sound=!sound;SetCan();if(sound)shakeAudio.PlayOneShot(shakeClip,.07f);}
  void SetCan(){if(band){band.material.SetColor("_BaseColor",GraffitiPalette.Colors[color]);band.material.SetColor("_EmissionColor",GraffitiPalette.Colors[color]*.08f);}for(int i=0;i<3;i++)if(caps[i])caps[i].gameObject.SetActive(i==cap);}
  void Haptic(float strength){if(dominant.isValid)dominant.SendHapticImpulse(0,strength,.07f);}
  void CreateFeedback(){
   var label=new GameObject("Graffiti guidance");status=label.AddComponent<TextMesh>();status.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");status.GetComponent<Renderer>().sharedMaterial=status.font.material;status.fontSize=64;status.characterSize=.003f;status.anchor=TextAnchor.MiddleCenter;status.alignment=TextAlignment.Center;status.color=new Color(.72f,.95f,1);
   cursor=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(cursor.GetComponent<Collider>());cursor.name="Wall spray focus";cursorMaterial=new Material(solidShader);cursorMaterial.SetColor("_BaseColor",new Color(.5f,.9f,1));cursor.GetComponent<Renderer>().sharedMaterial=cursorMaterial;
   var line=new GameObject("Palette pointer");rayLine=line.AddComponent<LineRenderer>();rayLine.positionCount=2;rayLine.widthMultiplier=.0014f;rayLine.sharedMaterial=cursorMaterial;
   sprayAudio=can.AddComponent<AudioSource>();sprayAudio.loop=true;sprayAudio.playOnAwake=false;sprayAudio.spatialBlend=.65f;sprayAudio.volume=0;shakeAudio=can.AddComponent<AudioSource>();shakeAudio.playOnAwake=false;shakeAudio.spatialBlend=.65f;
   var noise=new float[22050];var rng=new System.Random(104);float low=0;for(int i=0;i<noise.Length;i++){float n=(float)rng.NextDouble()*2-1;low=Mathf.Lerp(low,n,.12f);noise[i]=(n-low)*.28f;}sprayClip=AudioClip.Create("Aerosol hiss",noise.Length,1,22050,false);sprayClip.SetData(noise,0);sprayAudio.clip=sprayClip;
   var ball=new float[4400];for(int i=0;i<ball.Length;i++){float t=i/22050f;ball[i]=(Mathf.Sin(t*6300)+Mathf.Sin(t*9700)*.35f)*Mathf.Exp(-t*35)*.25f;}shakeClip=AudioClip.Create("Mixing ball",ball.Length,1,22050,false);shakeClip.SetData(ball,0);
   var pg=new GameObject("Subtle aerosol plume");aerosol=pg.AddComponent<ParticleSystem>();aerosol.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=aerosol.main;main.maxParticles=128;main.startSpeed=0;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=aerosol.emission;emission.enabled=false;var shape=aerosol.shape;shape.enabled=false;sprayMaterial=new Material(aerosolShader);pg.GetComponent<ParticleSystemRenderer>().sharedMaterial=sprayMaterial;aerosol.Play();
  }
  void OnApplicationFocus(bool focus){focused=focus;if(!focus){paint?.End();if(aerosol)aerosol.Clear();palette?.Hide();}else EnableRoom();}
  void OnDestroy(){paint?.Dispose();palette?.Dispose();PaintWall.Release(sprayClip);PaintWall.Release(shakeClip);PaintWall.Release(sprayMaterial);PaintWall.Release(cursorMaterial);}
 }
}
