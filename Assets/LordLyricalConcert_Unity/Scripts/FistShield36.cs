using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 public sealed class FistShield36:MonoBehaviour {
  readonly Transform[] discs=new Transform[3];readonly Material[] materials=new Material[3];
  readonly float[] visibility=new float[3];Mesh mesh;bool merged;
  public bool Merged=>merged;
  public void Initialize(){
   mesh=new Mesh{name="Palm guard dome"};var v=new Vector3[1+48*5];var uv=new Vector2[v.Length];var tri=new System.Collections.Generic.List<int>();
   v[0]=new Vector3(0,0,.12f);uv[0]=Vector2.one*.5f;
   for(int ring=0;ring<5;ring++)for(int i=0;i<48;i++){float r=(ring+1)/5f,a=i*Mathf.PI/24;int n=1+ring*48+i;v[n]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,(1-r*r)*.12f);uv[n]=new Vector2(v[n].x,v[n].y)*.5f+Vector2.one*.5f;int next=1+ring*48+(i+1)%48;if(ring==0){tri.Add(0);tri.Add(n);tri.Add(next);}else{int prev=n-48,pnext=next-48;tri.Add(prev);tri.Add(n);tri.Add(next);tri.Add(prev);tri.Add(next);tri.Add(pnext);}}
   mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri.ToArray();mesh.RecalculateBounds();
   for(int i=0;i<3;i++){var g=new GameObject(i==2?"United fist shield":"Fist shield "+i);g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();materials[i]=new Material(Resources.Load<Shader>("Cinema26/FistShield36"));r.sharedMaterial=materials[i];r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;discs[i]=g.transform;g.SetActive(false);}
  }
  public static bool Join(bool wasJoined,HandPoseInput.HandState l,HandPoseInput.HandState r)=>l.tracked&&r.tracked&&l.closed&&r.closed&&Vector3.Distance(l.position,r.position)<(wasJoined?.29f:.22f);
  public void Impact(float t){foreach(var m in materials)if(m)m.SetFloat("_ImpactAt",t);}
  public void Impact(float t,Vector3 point){Impact(t);for(int i=0;i<3;i++){var p=discs[i].InverseTransformPoint(point);materials[i].SetVector("_ImpactUV",new Vector4(Mathf.Clamp01(p.x*.5f+.5f),Mathf.Clamp01(p.y*.5f+.5f),0,0));}}
  public void Charge(bool ready){foreach(var m in materials)if(m)m.SetFloat("_Charge38",ready?1:0);}
  public void Render(float t,bool active,HandPoseInput hands,Color color,Camera camera)=>RenderPose(t,active&&camera,hands?hands.Left:default,hands?hands.Right:default,color,camera?camera.transform.position:Vector3.zero,Time.unscaledDeltaTime);
  public void RenderPose(float t,bool active,HandPoseInput.HandState l,HandPoseInput.HandState r,Color color,Vector3 head,float dt){
   merged=active&&Join(merged,l,r);
   for(int i=0;i<3;i++){
    bool on=active&&t>=26&&t<101&&(i==2?merged:!merged&&(i==0?l.tracked&&l.closed:r.tracked&&r.closed));
    visibility[i]=Mathf.MoveTowards(visibility[i],on?1:0,Mathf.Clamp(dt,0,.05f)*10);
    if(!active||(i<2&&!(i==0?l.tracked:r.tracked))||(i==2&&(!l.tracked||!r.tracked)))visibility[i]=0;
    discs[i].gameObject.SetActive(visibility[i]>.001f);if(visibility[i]<=0)continue;
    Vector3 centre=i==2?(l.position+r.position)*.5f:i==0?l.position:r.position;
    Vector3 normal=ShieldGeometry37.Normal(head,centre);
    discs[i].SetPositionAndRotation(ShieldGeometry37.Centre(head,centre,i==2),Quaternion.LookRotation(normal));
    discs[i].localScale=Vector3.one*(i==2?ShieldGeometry37.MergedRadius:ShieldGeometry37.SingleRadius)*Mathf.Lerp(.86f,1,visibility[i]);
    materials[i].SetColor("_Tint",color);materials[i].SetFloat("_Opacity",visibility[i]);materials[i].SetFloat("_Clock",t);
   }
  }
  void OnDestroy(){if(mesh)Destroy(mesh);foreach(var m in materials)if(m)Destroy(m);}
 }
}
