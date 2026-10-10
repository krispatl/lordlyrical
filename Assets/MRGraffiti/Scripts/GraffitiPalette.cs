using System;
using System.Collections.Generic;
using UnityEngine;
namespace MRGraffiti {
 public sealed class GraffitiPalette:IDisposable {
  public static readonly Color[] Colors={new Color(.015f,.018f,.025f),new Color(.94f,.94f,.9f),new Color(.85f,.02f,.03f),new Color(1,.18f,.02f),new Color(1,.72f,.01f),new Color(.5f,.9f,.01f),new Color(.02f,.55f,.12f),new Color(.01f,.8f,.65f),new Color(.01f,.65f,1),new Color(.04f,.15f,.9f),new Color(.07f,.025f,.3f),new Color(.4f,.03f,.8f),new Color(.9f,.02f,.6f),new Color(1,.25f,.48f),new Color(.55f,.61f,.69f),new Color(.85f,.5f,.16f)};
  public const float Radius=.16f;
  public static readonly Vector3 HandOffset=new Vector3(0,.15f,.045f);
  readonly GameObject root;readonly List<Button> buttons=new();readonly List<Material> materials=new();readonly List<Mesh> meshes=new();readonly Action<int> select;readonly Shader shader;readonly TextMesh heading,summary;
  readonly GameObject[] pages=new GameObject[4];Transform hand;Camera view;int page,selectedColor=8,selectedCap=1,selectedFinish;float clearUntil;bool requested;
  static readonly string[] Caps={"SKINNY","STANDARD","FAT"},Finishes={"MATTE","CHROME","HOLO"};
  sealed class Button {public int id,page;public Vector2 p,size;public float angle,span,inner,outer;public bool sector;public Material mat,border;public Color color;}
  public bool Open=>requested;public Transform Root=>root.transform;public float PointerDistance {get;private set;}=1;
  public GraffitiPalette(Shader shader,Action<int> select){
   this.shader=shader;this.select=select;root=new GameObject("CHROMA WALL | off-hand radial palette");
   Sector(root.transform,"Graphite wheel",0,360,0,Radius,new Color(.018f,.024f,.032f),.004f);
   Sector(root.transform,"Hub",0,360,0,.089f,new Color(.028f,.036f,.045f),0);
   heading=Text(root.transform,"CHROMA",new Vector2(0,.059f),.0025f);
   summary=Text(root.transform,"STANDARD / MATTE",new Vector2(0,.037f),.00145f);
   string[] tabs={"COLOUR","CAPS","FINISH","TOOLS"};
   for(int i=0;i<4;i++)Rectangle(100+i,new Vector2((i%2==0?-.034f:.034f),-.003f-(i/2)*.031f),new Vector2(.062f,.023f),tabs[i]);
   Text(root.transform,"A/X  CLOSE",new Vector2(0,-.062f),.0014f);
   for(int i=0;i<4;i++){pages[i]=new GameObject(tabs[i]+" wheel");pages[i].transform.SetParent(root.transform,false);}
   for(int i=0;i<16;i++)Wedge(0,i,i*22.5f,20,.1f,.153f,Colors[i],"");
   for(int i=0;i<3;i++)Wedge(1,20+i,i*120,116,.096f,.153f,new Color(.06f,.09f,.11f),Caps[i]);
   for(int i=0;i<3;i++)Wedge(2,30+i,i*120,116,.096f,.153f,new Color(.09f,.08f,.12f),Finishes[i]);
   string[] actions={"UNDO","CLEAR","MAP WALL","SCAN","SWAP HAND","SOUND"};
   for(int i=0;i<6;i++)Wedge(3,40+i,i*60,56,.096f,.153f,new Color(.045f,.065f,.08f),actions[i]);
   ShowPage(0);root.SetActive(false);
  }
  Material Mat(Color color){var m=new Material(shader);m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",0);materials.Add(m);return m;}
  Material Sector(Transform parent,string name,float angle,float span,float inner,float outer,Color color,float z){
   int steps=Mathf.Max(3,Mathf.CeilToInt(span/5));var v=new Vector3[(steps+1)*2];var tri=new int[steps*6];
   for(int i=0;i<=steps;i++){float a=(angle-span/2+span*i/steps)*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);v[i*2]=d*inner;v[i*2+1]=d*outer;if(i==steps)continue;int t=i*6,k=i*2;tri[t]=k;tri[t+1]=k+1;tri[t+2]=k+2;tri[t+3]=k+1;tri[t+4]=k+3;tri[t+5]=k+2;}
   var mesh=new Mesh{name=name};mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
   var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=Vector3.forward*z;g.AddComponent<MeshFilter>().sharedMesh=mesh;var m=Mat(color);g.AddComponent<MeshRenderer>().sharedMaterial=m;return m;
  }
  TextMesh Text(Transform parent,string text,Vector2 p,float size){var g=new GameObject(text);g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(p.x,p.y,-.004f);var t=g.AddComponent<TextMesh>();t.text=text;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.GetComponent<Renderer>().sharedMaterial=t.font.material;t.fontSize=64;t.characterSize=size;t.color=new Color(.85f,.92f,.96f);return t;}
  Material Plate(Vector2 p,Vector2 size,Color color,float z){var g=GameObject.CreatePrimitive(PrimitiveType.Quad);PaintWall.Release(g.GetComponent<Collider>());g.transform.SetParent(root.transform,false);g.transform.localPosition=new Vector3(p.x,p.y,z);g.transform.localScale=new Vector3(size.x,size.y,1);var m=Mat(color);g.GetComponent<Renderer>().sharedMaterial=m;return m;}
  void Rectangle(int id,Vector2 p,Vector2 size,string label){var c=new Color(.05f,.07f,.085f);var border=Plate(p,size+Vector2.one*.002f,c,.001f);var m=Plate(p,size,c,-.001f);buttons.Add(new Button{id=id,page=-1,p=p,size=size,color=c,mat=m,border=border});Text(root.transform,label,p,.00145f);}
  void Wedge(int page,int id,float angle,float span,float inner,float outer,Color c,string label){
   var parent=pages[page].transform;var border=Sector(parent,"Selection edge",angle,span+1,inner-.0015f,outer+.0015f,new Color(.05f,.075f,.085f),.001f);var m=Sector(parent,label==""?"Pigment "+id:label,angle,span,inner,outer,c,-.001f);
   buttons.Add(new Button{id=id,page=page,sector=true,angle=angle,span=span,inner=inner,outer=outer,color=c,mat=m,border=border});
   if(label!=""){float a=angle*Mathf.Deg2Rad;Text(parent,label,new Vector2(Mathf.Sin(a),Mathf.Cos(a))*(inner+outer)*.5f,label.Length>7?.00155f:.0019f);}
  }
  void ShowPage(int value){page=value;for(int i=0;i<4;i++)pages[i].SetActive(i==page);Refresh();}
  public void Toggle(Transform oppositeHand,Camera camera){if(requested){Hide();return;}hand=oppositeHand;view=camera;requested=true;root.transform.SetParent(hand,false);root.transform.localPosition=HandOffset;root.SetActive(true);Follow(true);}
  public void Follow(bool tracked){if(!requested)return;root.SetActive(tracked&&hand&&view);if(!root.activeSelf)return;root.transform.localPosition=HandOffset;var away=root.transform.position-view.transform.position;if(away.sqrMagnitude>.001f)root.transform.rotation=Quaternion.LookRotation(away,view.transform.up);}
  public bool Ray(Ray ray,bool click){
   PointerDistance=1;if(!requested||!root.activeInHierarchy)return false;
   if(clearUntil>0&&Time.unscaledTime>clearUntil){clearUntil=0;Refresh();}
   var tr=root.transform;float d=Vector3.Dot(ray.direction,tr.forward);if(Mathf.Abs(d)<.01f)return false;
   float length=Vector3.Dot(tr.position-ray.origin,tr.forward)/d;if(length<.015f||length>2)return false;var p=(Vector2)tr.InverseTransformPoint(ray.GetPoint(length));PointerDistance=length;
   float radius=p.magnitude,angle=Mathf.Atan2(p.x,p.y)*Mathf.Rad2Deg;Button chosen=null;
   foreach(var b in buttons){if(b.page>=0&&b.page!=page)continue;bool hover=b.sector?radius>=b.inner&&radius<=b.outer&&Mathf.Abs(Mathf.DeltaAngle(angle,b.angle))<b.span*.5f:Mathf.Abs(p.x-b.p.x)<b.size.x*.5f&&Mathf.Abs(p.y-b.p.y)<b.size.y*.5f;b.mat.SetColor("_BaseColor",b.color*(hover?1.45f:1));if(hover)chosen=b;}
   if(chosen==null)return radius<=Radius;if(!click)return true;
   int id=chosen.id;if(id>=100){clearUntil=0;ShowPage(id-100);return true;}
   if(id==41&&Time.unscaledTime>clearUntil){clearUntil=Time.unscaledTime+4;heading.text="CLEAR? AGAIN";return true;}
   clearUntil=0;if(id<16)selectedColor=id;else if(id>=20&&id<23)selectedCap=id-20;else if(id>=30&&id<33)selectedFinish=id-30;select(id);Refresh();return true;
  }
  void Refresh(){heading.text=clearUntil>0?"CLEAR? AGAIN":"CHROMA";summary.text=Caps[selectedCap]+" / "+Finishes[selectedFinish];foreach(var b in buttons){bool selected=b.id==selectedColor||b.id==20+selectedCap||b.id==30+selectedFinish||b.id==100+page;b.border.SetColor("_BaseColor",selected?new Color(.6f,.86f,.9f):new Color(.05f,.075f,.085f));}}
  public void Hide(){requested=false;clearUntil=0;root.SetActive(false);Refresh();}
  public void Dispose(){foreach(var m in materials)PaintWall.Release(m);foreach(var m in meshes)PaintWall.Release(m);PaintWall.Release(root);}
 }
}
