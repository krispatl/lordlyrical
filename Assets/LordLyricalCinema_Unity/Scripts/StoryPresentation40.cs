using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 // Cosmetic only. No new abilities, cue transitions, damage rules or locomotion.
 public sealed class StoryPresentation40:MonoBehaviour {
  public const int Budget=480,LineCount=23;
  readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[Budget];readonly LineRenderer[] lines=new LineRenderer[LineCount];readonly Vector3[] points=new Vector3[48];readonly Vector4[] colours=new Vector4[10];
  ParticleSystem particles;Material material;NetworkSessionController session;ConcertEncounter combat;int count;float quality=1,slowFor;
  public int ActiveParticles=>count;public int ActiveLines{get;private set;}
  public void Initialize(NetworkSessionController s,ConcertEncounter e){
   session=s;combat=e;material=new Material(Shader.Find("LordLyrical/Resonant Filament"));material.SetFloat("_CoreWhite",.45f);
   for(int i=0;i<LineCount;i++){var g=new GameObject(i<12?"Orc intent "+i:i<22?"Root Key witness "+(i-12):"Victory cymatics");g.transform.SetParent(transform,false);var l=lines[i]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.positionCount=48;l.useWorldSpace=true;l.loop=true;l.widthMultiplier=.009f;l.enabled=false;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;}
   var pg=new GameObject("Story40 | bounded musical fragments");pg.transform.SetParent(transform,false);particles=pg.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=particles.main;m.playOnAwake=false;m.loop=false;m.maxParticles=Budget;m.simulationSpace=ParticleSystemSimulationSpace.World;var em=particles.emission;em.enabled=false;var sh=particles.shape;sh.enabled=false;var r=particles.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Resources.Load<Material>("Concert/BeatAccents");r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.020f;particles.Play();particles.Pause();
   for(int i=0;i<10;i++)colours[i]=RootKeys.Slots[i].color;Shader.SetGlobalVectorArray("_RootColours40",colours);
  }
  void Add(Vector3 p,float size,float a,Color c){if(count>=Budget||a<.003f)return;c.a=a;buffer[count++]=new ParticleSystem.Particle{position=p,startSize=size,startColor=c,startLifetime=1,remainingLifetime=1,randomSeed=(uint)(count+1)};}
  public void Render(float t,bool showing){
   count=ActiveLines=0;bool live=showing&&t>=21&&t<111;
   if(Application.isPlaying){slowFor=Time.unscaledDeltaTime>.017f?slowFor+Time.unscaledDeltaTime:Mathf.Max(0,slowFor-Time.unscaledDeltaTime*2);quality=slowFor>1?.5f:Mathf.Min(1,quality+Time.unscaledDeltaTime*.1f);}
   int mask=0;foreach(var p in session.Participants.participants)if(p.connected&&p.slot>=0&&p.slot<10)mask|=1<<p.slot;if(session.Slot>=0&&session.Slot<10)mask|=1<<session.Slot;
   Shader.SetGlobalInt("_ActiveRootMask40",mask);float restore=0;foreach(var shot in combat.State.dragonShots)if(shot.blocked&&shot.resolved)restore=Mathf.Max(restore,Mathf.Exp(-Mathf.Max(0,t-shot.impactTime)*3));
   Shader.SetGlobalFloat("_Discord40",live?StoryScore40.Discord(t)*(1-restore):0);Shader.SetGlobalFloat("_StoryQuiet40",live?StoryScore40.Quiet(t):1);
   for(int i=0;i<LineCount;i++){
    float alpha=0;Vector3 centre=default;Color c=new Color(.87f,.73f,.47f);float radius=.7f;
    if(live&&i<12){var o=combat.State.orcs[i];alpha=Combat34Score.OrcWarning(o,t)*.42f;centre=o.position+Vector3.up*.025f;radius=o.intent==OrcIntent.Strike?1.0f:.72f;}
    else if(live&&i<22&&i>=12&&(mask&(1<<(i-12)))!=0){int slot=i-12;alpha=StoryScore40.Awakening(t)*.50f;centre=RootKeys.Slots[slot].spawn+Vector3.up*.03f;c=RootKeys.Slots[slot].color;radius=.48f;}
    else if(live&&i==22){alpha=StoryScore40.Art(t)*.22f;centre=new Vector3(0,.035f,10);radius=3.8f;}
    var l=lines[i];l.enabled=alpha>.003f;if(!l.enabled)continue;ActiveLines++;c.a=alpha;l.startColor=l.endColor=c;
    for(int j=0;j<48;j++){float a=j*Mathf.PI/24;float shape=i==22?1+.11f*Mathf.Cos(a*10)+.035f*Mathf.Sin(t*.2f+a*5):1+.045f*Mathf.Cos(a*6);points[j]=centre+new Vector3(Mathf.Cos(a)*radius*shape,0,Mathf.Sin(a)*radius*shape);}l.SetPositions(points);
   }
   if(live){
    float reveal=StoryScore40.Reveal(t);for(int i=0;i<Mathf.RoundToInt(100*quality)&&reveal>0;i++){float u=Mathf.Repeat(t*.22f+CinemaPresentation.Hash(i*61),1),a=i*2.399963f;Vector3 p=new Vector3(Mathf.Cos(a)*(3+i%3),4+u*5,13+Mathf.Sin(a)*2);Add(p,.012f,reveal*Mathf.Sin(u*Mathf.PI)*.32f*Presentation43Score.Decorative(t),new Color(.75f,.79f,.83f));}
    for(int i=0;i<12;i++){var o=combat.State.orcs[i];float age=t-o.defeatedAt;if(o.health<=0&&age>=0&&age<1.5f){for(int k=0;k<16;k++){float a=k*2.399963f,u=CinemaPresentation.Hash(k*71+i*43);Vector3 p=o.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*age*(.4f+u)+Presentation43Score.DeathDirection(o)*age*.45f+Vector3.up*(.2f+u*1.6f+age*.35f);Add(p,k%3==0?.022f:.010f,(1-age/1.5f)*.55f,k%3==0?new Color(.73f,.76f,.8f):RootKeys.Slots[Mathf.Clamp(o.lastHitSlot,0,9)].color);}}}
    float art=StoryScore40.Art(t);for(int i=0;i<Mathf.RoundToInt(120*quality)&&art>0;i++){int slot=i%10;if((mask&(1<<slot))==0)continue;float a=i*2.399963f,r=2+CinemaPresentation.Hash(i*73)*7;Vector3 p=new Vector3(Mathf.Cos(a)*r,.08f+CinemaPresentation.Hash(i*31)*.25f,10+Mathf.Sin(a)*r);Add(p,.008f,art*.18f*(.7f+.3f*MusicReactiveSystem.Current.pulse),RootKeys.Slots[slot].color);}
   }
   particles.SetParticles(buffer,count);particles.Simulate(0,false,false,false);
  }
  void OnDestroy(){Shader.SetGlobalFloat("_Discord40",0);Shader.SetGlobalFloat("_StoryQuiet40",1);Shader.SetGlobalInt("_ActiveRootMask40",0);if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
 }
}
