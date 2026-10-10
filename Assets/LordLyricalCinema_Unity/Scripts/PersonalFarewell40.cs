using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 public sealed class PersonalFarewell40:System.IDisposable {
  readonly GameObject root;readonly LineRenderer[] facets=new LineRenderer[4];readonly Material material;readonly LineRenderer trace;readonly Vector3[] ring=new Vector3[64];
  Vector3 anchor,burstOrigin;bool anchorSet,closedBefore;float previous=-100,burstAt=-100;
  readonly ParticleSystem.Particle[] sparks43=new ParticleSystem.Particle[32];ParticleSystem sparkSystem43;public int ActiveSparks{get;private set;}
  public float HandInfluence{get;private set;}
  public bool Visible{get;private set;}public Vector3 Position{get;private set;}
  public PersonalFarewell40(Transform parent){
   root=new GameObject("Root Key memory | personal farewell");root.transform.SetParent(parent,false);material=new Material(Shader.Find("LordLyrical/Resonant Filament"));material.SetColor("_Color",Color.white);material.SetFloat("_CoreWhite",.6f);
   for(int i=0;i<4;i++){var g=new GameObject("Memory facet "+i);g.transform.SetParent(root.transform,false);var l=facets[i]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.positionCount=5;l.useWorldSpace=true;l.widthMultiplier=.002f;l.enabled=false;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;}
   var tg=new GameObject("Landing remembered | fixed floor sigil");tg.transform.SetParent(root.transform,false);trace=tg.AddComponent<LineRenderer>();trace.sharedMaterial=material;trace.useWorldSpace=true;trace.loop=true;trace.positionCount=64;trace.widthMultiplier=.007f;trace.enabled=false;trace.shadowCastingMode=ShadowCastingMode.Off;trace.receiveShadows=false;
  }
  void Sparks(float t,bool active,Color colour,float squeeze){
   if(!sparkSystem43){var g=new GameObject("Farewell | closed-finger sparks");g.transform.SetParent(root.transform,false);sparkSystem43=g.AddComponent<ParticleSystem>();sparkSystem43.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=sparkSystem43.main;m.playOnAwake=false;m.maxParticles=32;m.loop=false;m.simulationSpace=ParticleSystemSimulationSpace.World;var e=sparkSystem43.emission;e.enabled=false;var sh=sparkSystem43.shape;sh.enabled=false;var r=sparkSystem43.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Resources.Load<Material>("Concert/BeatAccents");r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.012f;sparkSystem43.Play();sparkSystem43.Pause();}
   bool closed=squeeze>.5f;
   if(active&&Visible&&t>=120.8f&&t<123.6f&&closed&&!closedBefore&&t-burstAt>.7f){burstAt=t;burstOrigin=Position;}closedBefore=closed;
   ActiveSparks=0;float age=t-burstAt;
   if(active&&t<124&&age>=0&&age<.75f){for(int i=0;i<32;i++){float h=CinemaPresentation.Hash(i*71),a=i*2.399963f;Vector3 direction=new Vector3(Mathf.Cos(a),h*1.2f-.4f,Mathf.Sin(a));Color c=Color.Lerp(colour,Color.white,h*.45f);c.a=(1-age/.75f)*StoryScore40.Farewell(t)*.6f;sparks43[ActiveSparks++]=new ParticleSystem.Particle{position=burstOrigin+direction*age*.13f+Vector3.up*age*.025f,startSize=.003f+h*.004f,startColor=c,startLifetime=1,remainingLifetime=1,randomSeed=(uint)(i+1)};}}
   sparkSystem43.SetParticles(sparks43,ActiveSparks);sparkSystem43.Simulate(0,false,false,false);
  }
  public static Vector3 Target(float t,Vector3 founder,Vector3 palm,Vector3 normal)=>Vector3.Lerp(founder+Vector3.up*1.3f,palm+normal*.10f,CinemaTiming.S(119.2f,120.8f,t))+Vector3.up*Mathf.Sin(CinemaTiming.S(119.2f,120.8f,t)*Mathf.PI)*.16f;
  public void Render(float t,bool active,bool tracked,Vector3 palm,Vector3 normal,Vector3 founder,Color color){
   float a=active?StoryScore40.Farewell(t):0;Visible=tracked&&a>.002f;Position=Visible?Target(t,founder,palm,normal):Vector3.zero;
   Draw(t,active,color,0);
  }
  public void RenderInteractive(float t,bool active,Vector3 eye,Quaternion gaze,HandPoseInput.HandState left,HandPoseInput.HandState right,Vector3 founder,Color color,float dt){
   float a=active?StoryScore40.Farewell(t):0;Visible=a>.002f;HandInfluence=0;
   if(!Visible){Position=Vector3.zero;anchorSet=false;previous=t;closedBefore=false;burstAt=-100;Draw(t,active,color,0);Sparks(t,false,color,0);return;}
   Vector3 front=Farewell42.Front(eye,gaze);
   if(!anchorSet||t<previous||t-previous>.5f){anchor=front;anchorSet=true;closedBefore=false;burstAt=-100;}
   else anchor=Vector3.Lerp(anchor,front,1-Mathf.Exp(-Mathf.Clamp(dt,0,.05f)*1.8f));
   previous=t;
   Vector3 floating=anchor+Vector3.up*(Mathf.Sin((t-119.2f)*1.4f)*.014f);
   var chosen=Farewell42.Closest(floating,left,right);HandInfluence=chosen.tracked?Farewell42.Influence(floating,chosen.position)*CinemaTiming.S(120.3f,120.8f,t):0;
   Vector3 destination=Farewell42.Respond(floating,chosen,HandInfluence);
   Position=Vector3.Lerp(founder+Vector3.up*1.3f,destination,CinemaTiming.S(119.2f,120.3f,t));
   Draw(t,active,color,chosen.closed?HandInfluence:0);Sparks(t,active,color,chosen.closed?HandInfluence:0);
  }
  void Draw(float t,bool active,Color color,float squeeze){
   float a=active?StoryScore40.Farewell(t):0;
   for(int i=0;i<4;i++){var l=facets[i];l.enabled=Visible;if(!Visible)continue;Color c=Color.Lerp(color,new Color(.93f,.94f,.96f),i*.16f);c.a=a*.85f;l.startColor=l.endColor=c;Quaternion q=Quaternion.Euler(t*13+i*47,t*21+i*23,i*41);float r=(.022f+i*.006f)*Mathf.Lerp(1.25f,.72f,squeeze);for(int j=0;j<5;j++){float angle=j*Mathf.PI*.5f;l.SetPosition(j,Position+q*new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,0));}}
   float tracePower=active?StoryScore40.Trace(t):0;trace.enabled=tracePower>.002f;if(trace.enabled){var c=color;c.a=tracePower;trace.startColor=trace.endColor=c;for(int i=0;i<64;i++){float angle=i*Mathf.PI/32;float r=.55f*(1+.10f*Mathf.Cos(angle*6));ring[i]=new Vector3(Mathf.Cos(angle)*r,.024f,5+Mathf.Sin(angle)*r);}trace.SetPositions(ring);}
  }
  public void Dispose(){if(Application.isPlaying){Object.Destroy(root);Object.Destroy(material);}else{Object.DestroyImmediate(root);Object.DestroyImmediate(material);}}
 }
}
