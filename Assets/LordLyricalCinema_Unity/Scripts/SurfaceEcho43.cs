using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 // One retained contact glyph per participant and three fireball ground traces.
 public sealed class SurfaceEcho43:MonoBehaviour {
  public const int Count=13,Points=24;
  readonly LineRenderer[] lines=new LineRenderer[Count];readonly Vector3[] points=new Vector3[Points],centres=new Vector3[Count],normals=new Vector3[Count];readonly float[] times=new float[Count];readonly Color[] colours=new Color[Count];
  Material material;ConcertEncounter encounter;ManualResonanceBeams beams;float previous=-100;int run=-1;
  public int ActiveLines{get;private set;}
  public void Initialize(ConcertEncounter e,ManualResonanceBeams b){encounter=e;beams=b;material=new Material(Shader.Find("LordLyrical/Resonant Filament"));material.SetColor("_Color",Color.white);material.SetFloat("_CoreWhite",.2f);
   for(int i=0;i<Count;i++){times[i]=-100;var g=new GameObject(i<10?"Participant contact memory":"Fireball ground aftermath");g.transform.SetParent(transform,false);var l=lines[i]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=true;l.positionCount=Points;l.widthMultiplier=.007f;l.enabled=false;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;}
  }
  public void Render(float t,bool active){
   ActiveLines=0;bool live=active&&t>=21&&t<111;
   if(!live||t<previous||run!=encounter.State.run){for(int i=0;i<Count;i++)times[i]=-100;run=encounter.State.run;}previous=t;
   if(live){
    for(int slot=0;slot<10;slot++)for(int side=0;side<2;side++)if(beams&&beams.TryRenderedRay(slot,side,out var origin,out var direction)){
     var hit=BeamHitResolver.Trace(encounter.State,t,origin,direction);if(!hit.hit||hit.orc>=0)continue;
     // Match the rendered beam's foreground shield/heart interception.
     if(t>=75&&t<81||t>=92&&t<100){var target=t<82?encounter.State.shieldPoint:encounter.State.dragonHeart;if(ManualResonanceBeams.RaySphere(origin,direction,target,t<82?1.35f:1.7f,out float d)&&d<hit.distance)continue;}
     centres[slot]=hit.point+hit.normal*.016f;normals[slot]=hit.normal;times[slot]=t-.08f;colours[slot]=RootKeys.Slots[slot].color;
    }
    for(int i=0;i<3;i++){var shot=encounter.State.dragonShots[i];if(!shot.resolved||shot.blocked)times[10+i]=-100;if(shot.resolved&&!shot.blocked&&Presentation43Score.Scar(t-shot.impactTime)>0){centres[10+i]=new Vector3(shot.impactPoint.x,.028f,shot.impactPoint.z);normals[10+i]=Vector3.up;times[10+i]=shot.impactTime;colours[10+i]=new Color(1,.35f,.08f);}}
   }
   for(int i=0;i<Count;i++){float a=live?Presentation43Score.Scar(t-times[i]):0;var l=lines[i];l.enabled=a>.003f;if(!l.enabled)continue;ActiveLines++;Vector3 n=normals[i],x=Vector3.Cross(n,Vector3.up).normalized;if(x.sqrMagnitude<.01f)x=Vector3.right;Vector3 y=Vector3.Cross(n,x).normalized;float r=i<10?.105f:.38f;var c=colours[i];c.a=a*(i<10?.24f:.32f);l.startColor=l.endColor=c;
    for(int j=0;j<Points;j++){float angle=j*Mathf.PI*2/(Points-1),shape=1+.12f*Mathf.Cos(angle*6);points[j]=centres[i]+(x*Mathf.Cos(angle)+y*Mathf.Sin(angle))*r*shape;}l.SetPositions(points);
   }
  }
  void OnDisable(){ActiveLines=0;for(int i=0;i<Count;i++){times[i]=-100;if(lines[i])lines[i].enabled=false;}}
  void OnDestroy(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
 }
}
