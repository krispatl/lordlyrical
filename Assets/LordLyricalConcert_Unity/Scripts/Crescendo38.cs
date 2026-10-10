using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 // Fixed geometry; all motion derives from the shared song clock and authority state.
 public sealed class Crescendo38:MonoBehaviour {
  public const int RibbonCount=18,Points=24;
  readonly LineRenderer[] lines=new LineRenderer[RibbonCount];readonly Vector3[] points=new Vector3[Points];
  Material material;ConcertEncounter encounter;float quality=1,slowFor;
  readonly Gradient[] gradients43=new Gradient[10];readonly GradientColorKey[] colourKeys43=new GradientColorKey[2];readonly GradientAlphaKey[] alphaKeys43=new GradientAlphaKey[5];
  public int ActiveRibbons{get;private set;}
  public static float Envelope(float t,float charge){
   if(t<92||t>=101)return 0;
   return t<98?Mathf.SmoothStep(0,1,Mathf.Clamp01((t-92)/.6f))*Mathf.Clamp01(charge):Mathf.Exp(-(t-98)*2.4f);
  }
  public void Initialize(ConcertEncounter e){
   encounter=e;material=new Material(Shader.Find("LordLyrical/Resonant Filament"));material.SetColor("_Color",Color.white);material.SetFloat("_CoreWhite",.6f);
   for(int i=0;i<10;i++)gradients43[i]=new Gradient();
   for(int i=0;i<RibbonCount;i++){var g=new GameObject(i<10?"Participant convergence "+i:"Dragon heart fracture "+i);g.transform.SetParent(transform,false);var l=lines[i]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.positionCount=Points;l.useWorldSpace=true;l.enabled=false;l.widthMultiplier=.018f;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;l.numCapVertices=1;}
  }
  public void Render(float t,bool active){
   if(Application.isPlaying){slowFor=Time.unscaledDeltaTime>.017f?slowFor+Time.unscaledDeltaTime:Mathf.Max(0,slowFor-Time.unscaledDeltaTime*2);if(slowFor>1)quality=.5f;else if(slowFor==0)quality=Mathf.Min(1,quality+Time.unscaledDeltaTime*.1f);}
   float power=active?Envelope(t,encounter.State.finaleCharge):0;power*=StoryScore40.Quiet(t);ActiveRibbons=0;Shader.SetGlobalFloat("_Crescendo38",power*(.55f+.45f*MusicReactiveSystem.Current.pulse));
   for(int i=0;i<RibbonCount;i++){
    bool on=power>.002f&&(i<10?encounter.occupied[i]&&(t>=98||(encounter.State.supportMask&(1<<i))!=0):i<10+Mathf.RoundToInt(8*quality));
    var l=lines[i];l.enabled=on;if(!on)continue;ActiveRibbons++;
    Vector3 heart=encounter.State.dragonHeart;Color c=i<10?RootKeys.Slots[i].color:new Color(.92f,.76f,.44f);c.a=power*(i<10?.34f:.55f);l.startColor=l.endColor=c;
    if(i<10){float u=Mathf.Repeat((t-92)*.8f+i*.1f,1),lo=Mathf.Max(0,u-.08f),hi=Mathf.Min(1,u+.08f);
     colourKeys43[0]=new GradientColorKey(c,0);colourKeys43[1]=new GradientColorKey(c,1);
     alphaKeys43[0]=new GradientAlphaKey(c.a*.4f,0);alphaKeys43[1]=new GradientAlphaKey(c.a*.4f,lo);alphaKeys43[2]=new GradientAlphaKey(Mathf.Min(1,c.a*2),u);alphaKeys43[3]=new GradientAlphaKey(c.a*.4f,hi);alphaKeys43[4]=new GradientAlphaKey(c.a*.4f,1);
     gradients43[i].SetKeys(colourKeys43,alphaKeys43);l.colorGradient=gradients43[i];
    }
    l.widthMultiplier=(i<10?.013f:.023f)*(.65f+power)*(t>=98?1.8f:1);
    Vector3 start=i<10?encounter.heads[i]+Vector3.down*.5f:heart+Quaternion.Euler(i*31,i*47,0)*Vector3.forward*(.35f+power*.7f);
    for(int j=0;j<Points;j++){
     float u=j/(float)(Points-1),wave=Mathf.Sin(u*Mathf.PI);Vector3 p=Vector3.Lerp(start,heart,u);
     if(i<10){
      Vector3 shared=Vector3.Lerp(new Vector3(0,3.7f,6),heart,u);Vector3 axis=(heart-new Vector3(0,3.7f,6)).normalized,x=Vector3.Cross(axis,Vector3.up).normalized;if(x.sqrMagnitude<.01f)x=Vector3.right;Vector3 y=Vector3.Cross(axis,x);
      float phase=i*Mathf.PI/5+t*2+u*9;shared+=(x*Mathf.Cos(phase)+y*Mathf.Sin(phase))*(.10f+.04f*Mathf.Sin(u*Mathf.PI));p=Vector3.Lerp(p,shared,CinemaTiming.S(.38f,.8f,u));
     }
     if(i<10)p+=new Vector3(Mathf.Sin(u*12-t*4+i)*.10f,Mathf.Sin(u*8+t*2+i)*.09f,0)*wave;
     else p+=Quaternion.Euler(i*31,i*47,0)*new Vector3(Mathf.Sin(u*18+i)*.08f,Mathf.Sin(u*11+i)*.06f,0)*wave;
     points[j]=p;
    }l.SetPositions(points);
   }
  }
  void OnDestroy(){Shader.SetGlobalFloat("_Crescendo38",0);if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
 }
}
