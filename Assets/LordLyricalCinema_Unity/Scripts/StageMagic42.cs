using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 // Presentation only: consumes existing authority state and the offline analysed music clock.
 public sealed class StageMagic42:MonoBehaviour {
  public const int Budget=320,LineCount=10,LightCount=4;
  readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[Budget];readonly LineRenderer[] lines=new LineRenderer[LineCount];readonly Vector3[] points=new Vector3[32];readonly Light[] lights=new Light[LightCount];
  ParticleSystem particles;Material material;ConcertEncounter encounter;ChamberPresentation chamber;ResonantWorld world;TrackedRig rig;ManualResonanceBeams beams;Transform leftWing,rightWing;int count;float slowFor,quality=1;
  public int ActiveParticles=>count;public int ActiveLines{get;private set;}public int ActiveLights{get;private set;}
  public static float DragonPower(float t)=>BattleScore.W(63.4f,65,97.9f,101,t);
  public static float Charge(float t){float p=0;for(int i=0;i<DragonFireballScore.Count;i++)p=Mathf.Max(p,DragonFireballScore.Charge(t,i));return p;}
  public void Initialize(ConcertEncounter e,ChamberPresentation c,ResonantWorld w){
   encounter=e;chamber=c;world=w;beams=world.GetComponent<ManualResonanceBeams>();
   if(chamber)foreach(var bone in chamber.GetComponentsInChildren<Transform>(true)){if(bone.name=="wing_finger3_b.L")leftWing=bone;if(bone.name=="wing_finger3_b.R")rightWing=bone;}
   material=new Material(Shader.Find("LordLyrical/Resonant Filament"));material.SetColor("_Color",Color.white);material.SetFloat("_CoreWhite",.5f);
   for(int i=0;i<LineCount;i++){var g=new GameObject(i<3?"Dragon mouth sigil":i<5?"Wing frequency filament":i==5?"Bass breath on stone":"Percussive musical stroke");g.transform.SetParent(transform,false);var l=lines[i]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.positionCount=32;l.useWorldSpace=true;l.enabled=false;l.widthMultiplier=i<3?.012f:.006f;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;}
   for(int i=0;i<LightCount;i++){var g=new GameObject(i==0?"Lightning landing light":i==1?"Dragon charge light":"Beam contact light");g.transform.SetParent(transform,false);var l=lights[i]=g.AddComponent<Light>();l.type=LightType.Point;l.shadows=LightShadows.None;l.range=i<2?5:3.5f;l.intensity=0;l.enabled=false;}
   var pg=new GameObject("Magic42 | fixed frequency fragments");pg.transform.SetParent(transform,false);particles=pg.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=particles.main;m.playOnAwake=false;m.loop=false;m.maxParticles=Budget;m.simulationSpace=ParticleSystemSimulationSpace.World;var em=particles.emission;em.enabled=false;var shape=particles.shape;shape.enabled=false;var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("Concert/BeatAccents");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.03f;renderer.localBounds=new Bounds(new Vector3(0,8,12),new Vector3(45,35,60));particles.Play();particles.Pause();
  }
  void Add(Vector3 p,float size,float alpha,Color c){if(count>=Budget||alpha<.003f)return;c.a=alpha;buffer[count++]=new ParticleSystem.Particle{position=p,startSize=size,startColor=c,startLifetime=1,remainingLifetime=1,randomSeed=(uint)(count+1)};}
  void LightAt(int index,Vector3 p,Color c,float power){var l=lights[index];l.enabled=power>.003f;if(!l.enabled)return;ActiveLights++;l.transform.position=p;l.color=c;l.intensity=Mathf.Min(power,3);}
  public void Render(float t,bool showing,Vector3 mouth,Vector3 heart){
   count=ActiveLines=ActiveLights=0;Shader.SetGlobalFloat("_Song42",float.IsFinite(t)?t:0);
   if(!rig&&Application.isPlaying)rig=FindFirstObjectByType<TrackedRig>();
   if(Application.isPlaying){slowFor=Time.unscaledDeltaTime>.017f?slowFor+Time.unscaledDeltaTime:Mathf.Max(0,slowFor-Time.unscaledDeltaTime*2);quality=slowFor>1?.5f:Mathf.Min(1,quality+Time.unscaledDeltaTime*.1f);}
   var music=MusicReactiveSystem.Current;float dragon=showing?DragonPower(t):0,charge=dragon>0?Charge(t):0;float quiet=StoryScore40.Quiet(t)*Presentation43Score.Decorative(t);Color ice=new Color(.20f,.68f,1),silver=new Color(.76f,.82f,.90f);
   Vector3 forward=chamber?chamber.MouthForward:Vector3.back,x=Vector3.Cross(forward,Vector3.up).normalized;if(x.sqrMagnitude<.01f)x=Vector3.right;Vector3 y=Vector3.Cross(forward,x);
   for(int i=0;i<LineCount;i++){
    float alpha=0;var l=lines[i];
    if(i<3){alpha=dragon*charge*.25f;for(int j=0;j<32;j++){float u=j/31f,a=u*Mathf.PI*2+t*(.6f+i*.15f)+i*2;float r=.30f+i*.13f+.045f*Mathf.Sin(a*5+t);points[j]=mouth-forward*(.13f+i*.05f)+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*r;}}
    else if(i<5){var tip=i==3?leftWing:rightWing;if(tip&&dragon>0){alpha=dragon*quiet*.16f*(.6f+music.air*.4f);for(int j=0;j<32;j++){float u=j/31f;points[j]=Vector3.Lerp(heart,tip.position+tip.up*.7f,u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.12f+forward*Mathf.Sin(u*9-t*1.7f)*.035f;}}}
    else if(i==5){alpha=showing&&t>=21&&t<111?.09f*quiet*music.bass:0;for(int j=0;j<32;j++){float a=j*Mathf.PI*2/31;points[j]=new Vector3(Mathf.Cos(a)*(3+music.bass*.16f),.035f,10+Mathf.Sin(a)*(3+music.bass*.16f));}}
    else {alpha=showing&&t>=21&&t<111?.14f*quiet*music.pulse:0;Vector3 p=new Vector3((i-7.5f)*.52f,1.3f+(i%2)*.18f,4.2f+Mathf.Repeat(t*.12f+i*.17f,.6f));for(int j=0;j<32;j++){float u=j/31f;points[j]=p+new Vector3((u-.5f)*.16f,Mathf.Sin(u*Mathf.PI)*.025f,0);}}
    if(quality<.75f&&i>=7)alpha=0;l.enabled=alpha>.003f;if(!l.enabled)continue;ActiveLines++;Color color=i<5?ice:silver;color.a=alpha;l.startColor=l.endColor=color;l.SetPositions(points);
   }
   if(dragon>0){for(int i=0;i<Mathf.RoundToInt(144*quality);i++){float u=Mathf.Repeat(t*.18f+CinemaPresentation.Hash(i*31),1),a=i*2.399963f;Vector3 p=Vector3.Lerp(heart,mouth,u)+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*(.35f+.45f*Mathf.Sin(u*Mathf.PI));Add(p,.008f+charge*.01f,dragon*quiet*Mathf.Sin(u*Mathf.PI)*(.13f+charge*.18f),ice);}}
   if(showing&&t>=21&&t<111&&world.FounderRoot){for(int i=0;i<Mathf.RoundToInt(48*quality);i++){float u=Mathf.Repeat(t*.14f+CinemaPresentation.Hash(i*53),1),a=i*2.399963f;Add(world.FounderRoot.position+new Vector3(Mathf.Cos(a)*.65f,u*1.9f,Mathf.Sin(a)*.65f),.004f,quiet*music.mid*Mathf.Sin(u*Mathf.PI)*.13f,silver);}}
   float landing=BattleScore.W(-.86f,-.8f,-.64f,-.15f,t);LightAt(0,world.FounderRoot?world.FounderRoot.position+Vector3.up*1.2f:new Vector3(0,1.2f,5),silver,landing*2.5f);LightAt(1,mouth-forward*.12f,ice,dragon*(.22f+charge*2.2f));
   for(int side=0;side<2;side++){
    var h=rig&&rig.Hands?(side==0?rig.Hands.Left:rig.Hands.Right):default;float power=0;Vector3 point=default;Color color=Shader.GetGlobalColor("_ParticipantTint");
    if(showing&&beams&&beams.TryRenderedRay(beams.LocalSlot,side,out var origin,out var direction)&&PalmBeamTiming.Allowed(t)){var hit=BeamHitResolver.Trace(encounter.State,t,origin,direction);if(hit.hit){point=hit.point+hit.normal*.10f;power=.65f+.35f*music.pulse;}}
    if(showing&&side==0&&power==0)foreach(var shot in encounter.State.dragonShots)if(shot.resolved){float age=t-shot.impactTime;if(age>=0&&age<.3f){point=shot.impactPoint;power=Mathf.Max(power,(1-age/.3f)*1.3f);color=shot.blocked&&shot.blockerSlot>=0?RootKeys.Slots[shot.blockerSlot].color:new Color(1,.35f,.08f);}}
    LightAt(side+2,point,color,power*(quality<.75f&&side==1?0:1));
   }
   particles.SetParticles(buffer,count);particles.Simulate(0,false,false,false);
  }
  void OnDisable(){foreach(var l in lights)if(l)l.enabled=false;foreach(var l in lines)if(l)l.enabled=false;if(particles)particles.Clear();count=ActiveLights=ActiveLines=0;}
  void OnDestroy(){Shader.SetGlobalFloat("_Song42",0);if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
 }
}
