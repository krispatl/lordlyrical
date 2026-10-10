using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace MRGraffiti {
 // A single bounded draw for moving wet tips. Trails remain in the wall's paint buffers.
 public sealed class DripHeads:IDisposable {
  readonly GameObject root;readonly Mesh mesh;readonly Material material;readonly Vector3[] vertices;readonly Vector2[] uv;readonly Color[] colors;int count;
  public DripHeads(Shader shader,int limit){
   root=new GameObject("Wet paint droplet tips");mesh=new Mesh{name="Wall attached wet tips"};mesh.MarkDynamic();material=new Material(shader);
   vertices=new Vector3[limit*4];uv=new Vector2[limit*4];colors=new Color[limit*4];var tris=new int[limit*6];
   for(int i=0;i<limit;i++){int v=i*4,t=i*6;uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.up;uv[v+3]=Vector2.one;tris[t]=v;tris[t+1]=v+2;tris[t+2]=v+1;tris[t+3]=v+1;tris[t+4]=v+2;tris[t+5]=v+3;}
   mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=tris;root.AddComponent<MeshFilter>().sharedMesh=mesh;var r=root.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
  }
  public void Begin(){count=0;}
  public void Add(PaintWall wall,Vector2 at,Vector2 gravity,float radius,Color color,float age){
   if(count*4>=vertices.Length)return;Vector3 p=wall.root.TransformPoint(new Vector3((at.x-.5f)*wall.size.x,(at.y-.5f)*wall.size.y,0));
   Vector3 down=wall.root.TransformDirection(new Vector3(gravity.x,gravity.y,0))*radius*1.6f,side=wall.root.TransformDirection(new Vector3(-gravity.y,gravity.x,0))*radius;
   int k=count++*4;vertices[k]=p-side-down;vertices[k+1]=p+side-down;vertices[k+2]=p-side+down;vertices[k+3]=p+side+down;
   color.a=Mathf.Clamp01(age*6);for(int j=0;j<4;j++)colors[k+j]=color;
  }
  public void End(){for(int i=count*4;i<vertices.Length;i++){vertices[i]=Vector3.zero;colors[i]=Color.clear;}mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();root.SetActive(count>0);}
  public void Clear(){Begin();End();}
  public void Dispose(){PaintWall.Release(root);PaintWall.Release(mesh);PaintWall.Release(material);}
 }
}
