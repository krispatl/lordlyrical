using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    public static class ResonantGeometry
    {
        public static Material Material(string name,Color color,float glow=.5f)
        {var m=new Material(Shader.Find("LordLyrical/Resonance")){name=name,enableInstancing=true};m.SetColor("_Color",color);m.SetFloat("_Glow",glow);return m;}
        public static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material,Vector3 position,Vector3 scale)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return g;}
        public static Mesh Ring(float radius,float thickness,int segments=96)
        {
            var v=new Vector3[segments*2];var uv=new Vector2[v.Length];var t=new int[segments*6];
            for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;for(int j=0;j<2;j++){float r=radius+(j-.5f)*thickness;v[i*2+j]=new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);uv[i*2+j]=new Vector2(i/(float)segments,j);}int k=i*6,n=(i+1)%segments;t[k]=i*2;t[k+1]=n*2;t[k+2]=i*2+1;t[k+3]=i*2+1;t[k+4]=n*2;t[k+5]=n*2+1;}
            var mesh=new Mesh{name="Resonant ring",vertices=v,uv=uv,triangles=t};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Lathe(float[] heights,float[] radii,int segments=12)
        {
            int rows=heights.Length;var v=new Vector3[rows*(segments+1)];var uv=new Vector2[v.Length];var t=new int[(rows-1)*segments*6];
            for(int y=0;y<rows;y++)for(int x=0;x<=segments;x++){float a=x*2*Mathf.PI/segments;int n=y*(segments+1)+x;v[n]=new Vector3(Mathf.Cos(a)*radii[y],heights[y],Mathf.Sin(a)*radii[y]);uv[n]=new Vector2(x/(float)segments,y/(float)(rows-1));}
            int k=0;for(int y=0;y<rows-1;y++)for(int x=0;x<segments;x++){int a=y*(segments+1)+x,b=a+segments+1;t[k++]=a;t[k++]=b;t[k++]=a+1;t[k++]=a+1;t[k++]=b;t[k++]=b+1;}
            var m=new Mesh{name="Sculpted resonance form",vertices=v,uv=uv,triangles=t};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static Mesh Tube(Vector3[] points,float[] radii,int sides=10)
        {
            var v=new Vector3[points.Length*(sides+1)];var uv=new Vector2[v.Length];var t=new int[(points.Length-1)*sides*6];
            for(int i=0;i<points.Length;i++){var direction=points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)];var q=Quaternion.FromToRotation(Vector3.up,direction.normalized);for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;int n=i*(sides+1)+j;v[n]=points[i]+q*new Vector3(Mathf.Sin(a)*radii[i],0,Mathf.Cos(a)*radii[i]);uv[n]=new Vector2(j/(float)sides,i/(float)points.Length);}}
            int k=0;for(int i=0;i<points.Length-1;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;t[k++]=a;t[k++]=a+1;t[k++]=b;t[k++]=b;t[k++]=a+1;t[k++]=b+1;}
            var m=new Mesh{name="Frequency spine",vertices=v,uv=uv,triangles=t};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static LineRenderer Line(string name,Transform parent,Material material,int count,float width)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=true;l.positionCount=count;l.widthMultiplier=width;l.numCapVertices=0;l.shadowCastingMode=ShadowCastingMode.Off;return l;}
        public static Transform Node(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    }
}
