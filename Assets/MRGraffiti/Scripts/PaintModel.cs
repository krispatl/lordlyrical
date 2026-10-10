using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MRGraffiti {
 [Serializable] public struct Cap {
  public string name; public float cone,flow,softness,overspray,pressure,wetness,range,wideFlare;
  public static Cap[] Defaults=>new[]{new Cap{name="SKINNY",cone=3,flow=.65f,softness=.25f,overspray=.12f,pressure=1.6f,wetness=.65f,range=1.8f},new Cap{name="STANDARD",cone=7,flow=1,softness=.5f,overspray=.25f,pressure=1.35f,wetness=1,range=2.2f},new Cap{name="FAT",cone=13,flow=1.65f,softness=.75f,overspray=.4f,pressure=1.1f,wetness=1.5f,range=2.5f,wideFlare=1}};
 }
 public static class SprayModel {
  public static float Flow(Cap cap,float trigger,float distance,float incidence,float speed)=>Mathf.Pow(Mathf.InverseLerp(.06f,1,trigger),cap.pressure)*cap.flow*Mathf.Clamp01(incidence)*Mathf.Pow(Mathf.Clamp01(1-distance/cap.range),.7f)/(1+distance*distance*4)/(1+Mathf.Max(0,speed)*.22f);
  public static float Radius(Cap cap,float distance){
   float radius=.003f+Mathf.Tan(cap.cone*Mathf.Deg2Rad)*Mathf.Max(.02f,distance);
   // Fat cap opens gradually from a controlled close fill to a broad distant fan.
   float flare=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.9f,distance));
   return radius*Mathf.Lerp(1,Mathf.Lerp(2.04f,3.36f,flare),cap.wideFlare);
  }
 }
 public sealed class PaintWall:IDisposable {
  public readonly Transform root;public readonly Vector2 size;public readonly RenderTexture pigment,properties;public readonly Material material;readonly Mesh mesh;
  public ulong anchor;public bool painted;public readonly float[] wet=new float[64*64],wetAt=new float[64*64];
  public PaintWall(Transform parent,Vector3 position,Quaternion rotation,Vector2 extent,Shader shader){
   size=extent;var g=new GameObject("Paintable wall");root=g.transform;root.SetParent(parent,false);root.localPosition=position;root.localRotation=rotation;
   pigment=Texture(RenderTextureFormat.ARGB32);properties=Texture(RenderTextureFormat.ARGBHalf);Clear();
   mesh=new Mesh{name="Surface attached paint"};mesh.vertices=new[]{new Vector3(-size.x/2,-size.y/2,0),new Vector3(size.x/2,-size.y/2,0),new Vector3(-size.x/2,size.y/2,0),new Vector3(size.x/2,size.y/2,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
   g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();material=new Material(shader);material.SetTexture("_Paint",pigment);material.SetTexture("_Properties",properties);renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  }
  public static RenderTexture Texture(RenderTextureFormat format){var rt=new RenderTexture(1024,1024,0,format,RenderTextureReadWrite.Linear){name="Wall paint buffer",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,useMipMap=false};rt.Create();return rt;}
  public bool Hit(Ray ray,float range,out Vector2 uv,out Vector3 point,out float distance,out float incidence){
   uv=default;point=default;distance=0;incidence=0;Vector3 normal=root.forward;float d=Vector3.Dot(ray.direction,normal);if(Mathf.Abs(d)<.12f)return false;
   distance=Vector3.Dot(root.position-ray.origin,normal)/d;if(distance<.025f||distance>range)return false;
   point=ray.GetPoint(distance);var p=root.InverseTransformPoint(point);uv=new Vector2(p.x/size.x+.5f,p.y/size.y+.5f);incidence=Mathf.Abs(d);return uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1;
  }
  public void Clear(){ClearRT(pigment);ClearRT(properties);Array.Clear(wet,0,wet.Length);Array.Clear(wetAt,0,wetAt.Length);painted=false;}
  public static void ClearRT(RenderTexture rt){var old=RenderTexture.active;RenderTexture.active=rt;GL.Clear(false,true,Color.clear);RenderTexture.active=old;}
  public void Dispose(){pigment.Release();properties.Release();Release(pigment);Release(properties);Release(material);Release(mesh);Release(root.gameObject);}
  public static void Release(UnityEngine.Object o){if(!o)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
 }
 public sealed class PaintEngine:IDisposable {
  public const int UndoLimit=6,MaxDrips=12;readonly Material brush;readonly Mesh quad;readonly Vector3[] vertices=new Vector3[4];readonly List<Snapshot> history=new();readonly List<Drip> drips=new();
  PaintWall stroke;Vector2 previous;bool previousValid;int seed;float dwell;
  sealed class Snapshot {public PaintWall wall;public RenderTexture paint,properties;public readonly List<Drip> drops=new();}
  sealed class Drip {public PaintWall wall;public Vector2 uv,gravity;public Color color;public float age,finish,radius;public Drip Copy()=>(Drip)MemberwiseClone();}
  public int UndoCount=>history.Count;public int DripCount=>drips.Count;public int StampCount {get;private set;}
  readonly DripHeads heads;
  public PaintEngine(Shader shader,Shader dripShader=null){if(dripShader)heads=new DripHeads(dripShader,MaxDrips);brush=new Material(shader);quad=new Mesh{name="Bounded aerosol stamp"};quad.vertices=new Vector3[4];quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};quad.triangles=new[]{0,2,1,1,2,3};}
  public void End(){stroke=null;previousValid=false;dwell=0;}
  void Begin(PaintWall wall){stroke=wall;previousValid=false;if(history.Count>=UndoLimit){Drop(history[0]);history.RemoveAt(0);}var s=new Snapshot{wall=wall,paint=PaintWall.Texture(RenderTextureFormat.ARGB32),properties=PaintWall.Texture(RenderTextureFormat.ARGBHalf)};Copy(wall.pigment,s.paint);Copy(wall.properties,s.properties);foreach(var d in drips)if(d.wall==wall)s.drops.Add(d.Copy());history.Add(s);}
  public void Spray(PaintWall wall,Vector2 uv,Vector3 direction,float distance,float incidence,float trigger,float speed,float dt,Cap cap,Color color,int finish){
   float flow=SprayModel.Flow(cap,trigger,distance,incidence,speed);if(flow<=.0001f||dt<=0)return;if(stroke==null)Begin(wall);if(stroke!=wall)return;
   float radius=SprayModel.Radius(cap,distance),travel=previousValid?Vector2.Scale(uv-previous,wall.size).magnitude:0;
   if(travel>.6f)previousValid=false;
   int steps=previousValid?Mathf.Clamp(Mathf.CeilToInt(travel/Mathf.Max(.003f,radius*.35f)),1,24):1;
   var tangent=wall.root.InverseTransformDirection(direction);Vector2 axis=new Vector2(tangent.x,tangent.y).normalized;if(axis.sqrMagnitude<.01f)axis=Vector2.right;
   for(int j=1;j<=steps;j++)Stamp(wall,previousValid?Vector2.Lerp(previous,uv,j/(float)steps):uv,radius,1/Mathf.Max(.22f,incidence),axis,flow*Mathf.Min(dt,.05f)*22/steps,cap.softness,cap.overspray,color,finish);
   previous=uv;previousValid=true;wall.painted=true;
   int x=Mathf.Clamp((int)(uv.x*64),0,63),y=Mathf.Clamp((int)(uv.y*64),0,63),cell=x+y*64;
   wall.wet[cell]*=Mathf.Exp(-(Time.time-wall.wetAt[cell])/6);wall.wetAt[cell]=Time.time;wall.wet[cell]+=flow*dt*cap.wetness;
   dwell+=dt;
   if(wall.wet[cell]>1.3f&&speed<.18f&&dwell>.6f&&drips.Count<MaxDrips){
    Vector3 gravity=wall.root.InverseTransformDirection(Physics.gravity);Vector2 g=new Vector2(gravity.x,gravity.y);
    if(g.magnitude>.1f){drips.Add(new Drip{wall=wall,uv=uv,gravity=g.normalized,color=color,finish=finish,radius=Mathf.Lerp(.0025f,.0045f,Mathf.Clamp01(cap.wetness- .5f))});wall.wet[cell]*=.35f;dwell=0;}
   }
  }
  void Stamp(PaintWall wall,Vector2 uv,float radius,float stretch,Vector2 axis,float dose,float softness,float overspray,Color color,float finish){
   Vector2 x=Vector2.Scale(axis*radius*stretch,new Vector2(1/wall.size.x,1/wall.size.y)),y=Vector2.Scale(new Vector2(-axis.y,axis.x)*radius,new Vector2(1/wall.size.x,1/wall.size.y));
   Set(0,uv-x-y);Set(1,uv+x-y);Set(2,uv-x+y);Set(3,uv+x+y);quad.vertices=vertices;quad.RecalculateBounds();
   brush.SetColor("_Color",color);brush.SetVector("_Spray",new Vector4(Mathf.Clamp(dose,0,.85f),softness,overspray,++seed%997));brush.SetVector("_Finish",new Vector4(finish==1?1:0,finish==2?1:0,Time.time,0));
   var old=RenderTexture.active;Graphics.SetRenderTarget(wall.pigment);brush.SetPass(0);Graphics.DrawMeshNow(quad,Matrix4x4.identity);Graphics.SetRenderTarget(wall.properties);brush.SetPass(1);Graphics.DrawMeshNow(quad,Matrix4x4.identity);RenderTexture.active=old;StampCount++;
  }
  void Set(int i,Vector2 uv){vertices[i]=new Vector3(uv.x*2-1,uv.y*2-1,0);}
  public void Tick(float dt){
   dt=Mathf.Clamp(dt,0,.05f);heads?.Begin();
   for(int i=drips.Count-1;i>=0;i--){
    var d=drips[i];d.age+=dt;
    if(d.age>=5){Stamp(d.wall,d.uv,d.radius,1.35f,d.gravity,.75f,.12f,0,d.color,d.finish);drips.RemoveAt(i);continue;}
    float speed=.115f*(1-Mathf.Exp(-d.age*5))*Mathf.Exp(-d.age*.30f);
    Vector2 step=Vector2.Scale(d.gravity,new Vector2(1/d.wall.size.x,1/d.wall.size.y))*speed*dt,next=d.uv+step;
    Vector2 margin=new Vector2(d.radius*2/d.wall.size.x,d.radius*2/d.wall.size.y);
    if(next.x<margin.x||next.x>1-margin.x||next.y<margin.y||next.y>1-margin.y){drips.RemoveAt(i);continue;}
    int steps=Mathf.Clamp(Mathf.CeilToInt(speed*dt/(d.radius*.45f)),1,8);
    for(int j=1;j<=steps;j++)Stamp(d.wall,Vector2.Lerp(d.uv,next,j/(float)steps),d.radius*.68f,1,d.gravity,.32f,.18f,0,d.color,d.finish);
    d.uv=next;heads?.Add(d.wall,d.uv,d.gravity,d.radius,d.color,d.age);
   }
   heads?.End();
  }
  public Vector2 FirstDripUV=>drips.Count>0?drips[0].uv:Vector2.zero;
  public void Undo(){End();if(history.Count==0)return;var s=history[history.Count-1];drips.RemoveAll(d=>d.wall==s.wall);foreach(var d in s.drops)if(drips.Count<MaxDrips)drips.Add(d.Copy());heads?.Clear();Copy(s.paint,s.wall.pigment);Copy(s.properties,s.wall.properties);Array.Clear(s.wall.wet,0,s.wall.wet.Length);history.RemoveAt(history.Count-1);Drop(s);}
  public void Clear(IReadOnlyList<PaintWall> walls){End();drips.Clear();heads?.Clear();foreach(var s in history)Drop(s);history.Clear();foreach(var w in walls)w.Clear();}
  static void Copy(RenderTexture source,RenderTexture target){var prior=RenderTexture.active;Graphics.Blit(source,target);RenderTexture.active=prior;}
  static void Drop(Snapshot s){s.paint.Release();s.properties.Release();PaintWall.Release(s.paint);PaintWall.Release(s.properties);}
  public void Dispose(){heads?.Dispose();foreach(var s in history)Drop(s);PaintWall.Release(brush);PaintWall.Release(quad);}
 }
}
