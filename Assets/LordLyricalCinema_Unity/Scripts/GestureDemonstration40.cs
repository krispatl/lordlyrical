using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 public sealed class GestureDemonstration40:MonoBehaviour {
  readonly LineRenderer[] outlines=new LineRenderer[2];readonly Vector3[] points=new Vector3[28];Material material;
  public void Initialize(){material=new Material(Shader.Find("LordLyrical/Resonant Filament"));for(int i=0;i<2;i++){var g=new GameObject("Gesture demonstration hand "+i);g.transform.SetParent(transform,false);var l=outlines[i]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.positionCount=28;l.loop=true;l.useWorldSpace=false;l.widthMultiplier=.0025f;l.enabled=false;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;}}
  public void Render(float t,float alpha){
   int gesture=Mathf.Clamp((int)((t-21)/2),0,2);float folded=gesture==0?0:CinemaTiming.S(0,.5f,Mathf.Repeat(t-21,2));
   for(int side=0;side<2;side++){var l=outlines[side];l.enabled=alpha>.001f&&(side==0||gesture==2);if(!l.enabled)continue;Color c=new Color(.88f,.91f,.94f,alpha*.7f);l.startColor=l.endColor=c;float offset=gesture==2?(side==0?-.075f:.075f):0;
    points[0]=new Vector3(offset-.04f,.20f,0);points[1]=new Vector3(offset-.055f,.245f,0);points[2]=new Vector3(offset-.078f,.275f,0);points[3]=new Vector3(offset-.066f,.285f,0);points[4]=new Vector3(offset-.04f,.268f,0);int k=5;
    for(int finger=0;finger<4;finger++){float x=offset-.034f+finger*.022f,height=Mathf.Lerp(.34f-(finger==0||finger==3?.018f:0),.275f,folded);points[k++]=new Vector3(x,.27f,0);points[k++]=new Vector3(x,height,0);points[k++]=new Vector3(x+.009f,height+.003f,0);points[k++]=new Vector3(x+.014f,height,0);points[k++]=new Vector3(x+.014f,.27f,0);}
    points[25]=new Vector3(offset+.044f,.24f,0);points[26]=new Vector3(offset+.03f,.20f,0);points[27]=points[0];l.SetPositions(points);
   }
  }
  void OnDestroy(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
 }
}
