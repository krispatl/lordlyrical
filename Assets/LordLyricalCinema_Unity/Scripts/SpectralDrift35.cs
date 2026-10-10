using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    // Original implementation: prerecorded FFT, evaluated by shared song time (no microphone).
    public static class TrackSpectrum35
    {
        static ushort[] data;static int frames,rate,bands;
        public static bool Ready {get {Load();return data!=null;}}
        static void Load(){
            if(data!=null)return;var asset=Resources.Load<TextAsset>("Cinema26/Spectrum35");if(!asset)return;
            var b=asset.bytes;if(b.Length<16||BitConverter.ToInt32(b,0)!=3501)return;
            rate=BitConverter.ToInt32(b,4);bands=BitConverter.ToInt32(b,8);frames=BitConverter.ToInt32(b,12);
            if(rate!=30||bands!=24||frames<2||b.Length!=16+frames*bands*2)return;
            data=new ushort[frames*bands];Buffer.BlockCopy(b,16,data,0,data.Length*2);
        }
        public static float Sample(float t,float band){
            Load();if(data==null||t<0||t>=(frames-1)/(float)rate)return 0;
            float frame=Mathf.Min(t*rate,frames-1);int f=Math.Min((int)frame,frames-2);float b=Mathf.Clamp(band,0,bands-1);int k=(int)b,k1=Math.Min(k+1,bands-1);
            float a=Mathf.Lerp(data[f*bands+k],data[f*bands+k1],b-k);
            float c=Mathf.Lerp(data[(f+1)*bands+k],data[(f+1)*bands+k1],b-k);
            return Mathf.Lerp(a,c,frame-f)/65535f;
        }
    }
    public sealed class SpectralDrift35:IDisposable
    {
        public const int MaxFilaments=72,Segments=24;
        readonly GameObject root;readonly Mesh mesh;readonly Material material;readonly MeshRenderer renderer;
        readonly List<Vector3> points=new(MaxFilaments*Segments*4);
        readonly List<Color> colors=new(MaxFilaments*Segments*4);
        readonly List<Vector2> uv=new(MaxFilaments*Segments*4);
        readonly List<int> indices=new(MaxFilaments*Segments*6);
        public int ActiveFilaments {get;private set;} public int VertexCount=>points.Count;
        public Vector3 InspectPoint {get;private set;}
        public SpectralDrift35(Transform parent){
            root=new GameObject("Sound microstructure | drifting spectral filaments");root.transform.SetParent(parent,false);
            mesh=new Mesh{name="Spectral ribbons | one bounded batch"};mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=root.AddComponent<MeshRenderer>();
            material=new Material(Resources.Load<Shader>("Cinema26/SpectralThread35"));renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        public static Vector3 Centre(int i,float t){
            float h=CinemaPresentation.Hash(i*97+71),p=Mathf.Repeat(h+t*.019f,1);
            float x=(CinemaPresentation.Hash(i*37+9)*2-1)*4.1f;
            float z=7-p*10;
            // Reserve the central performer sightline; this field lives in space, not on the head.
            if(Mathf.Abs(x)<.7f)x=(x<0?-1:1)*(.8f+Mathf.Abs(x));
            return new Vector3(x+.10f*Mathf.Sin(t*.27f+i),.65f+CinemaPresentation.Hash(i*53+1)*1.45f+.045f*Mathf.Sin(t*.39f+i),z);
        }
        public void Render(float t,bool live,Camera camera,int quality){
            points.Clear();colors.Clear();uv.Clear();indices.Clear();ActiveFilaments=0;
            float envelope=CinemaTiming.S(.8f,4,t)*(1-CinemaTiming.S(CinemaTiming.FounderFadeStart,CinemaTiming.FounderGone,t));
            if(live&&camera&&envelope>.001f){
                int count=quality==0?36:MaxFilaments;Vector3 eye=camera.transform.position;
                for(int i=0;i<count;i++){
                    Vector3 centre=Centre(i,t);if(i==0)InspectPoint=centre;
                    float distance=Vector3.Distance(eye,centre),phase=Mathf.Repeat(CinemaPresentation.Hash(i*97+71)+t*.019f,1);
                    float alpha=envelope*CinemaTiming.S(.12f,.28f,distance)*(1-CinemaTiming.S(3.5f,6,distance))*CinemaTiming.S(0,.06f,phase)*(1-CinemaTiming.S(.94f,1,phase));
                    alpha*=StoryScore40.Quiet(t)*Magic33Score.Quiet(t)*(t>=63&&t<101?.45f:1);
                    if(alpha<.003f)continue;ActiveFilaments++;
                    float band=(i%8)*2.7f,energy=TrackSpectrum35.Sample(t,band);
                    float length=.12f+CinemaPresentation.Hash(i*31+5)*.12f;
                    Quaternion rotation=Quaternion.Euler(Mathf.Sin(i)*18,i*137.5f,Mathf.Sin(i*.7f)*24);
                    Vector3 prev=Point(0);
                    for(int j=1;j<=Segments;j++){
                        float u=j/(float)Segments;Vector3 next=Point(u),mid=(prev+next)*.5f;
                        Vector3 side=Vector3.Cross(next-prev,eye-mid).normalized*(.0008f+energy*.00045f);
                        Color c=Color.Lerp(new Color(.18f,.65f,1),new Color(1,.67f,.3f),i%5==0?.9f:0);
                        c.a=alpha*(.35f+energy*.55f)*Mathf.Pow(Mathf.Sin((u-.5f/Segments)*Mathf.PI),.45f);
                        int n=points.Count;points.Add(prev-side);points.Add(prev+side);points.Add(next+side);points.Add(next-side);
                        colors.Add(c);colors.Add(c);colors.Add(c);colors.Add(c);
                        uv.Add(Vector2.zero);uv.Add(Vector2.up);uv.Add(Vector2.one);uv.Add(Vector2.right);
                        indices.Add(n);indices.Add(n+1);indices.Add(n+2);indices.Add(n);indices.Add(n+2);indices.Add(n+3);prev=next;
                    }
                    Vector3 Point(float u){
                        float spectrum=TrackSpectrum35.Sample(t,Mathf.Repeat(band+u*7,23));
                        float wave=Mathf.Sin(u*Mathf.PI*(4+i%4*2)+t*(1.1f+band*.035f)+i);
                        float detail=Mathf.Sin(u*Mathf.PI*16-t*.8f)*TrackSpectrum35.Sample(t,20)*.003f;
                        return centre+rotation*new Vector3((u-.5f)*length,wave*(.003f+spectrum*.018f)+detail,Mathf.Cos(u*6.283f+t*.3f)*spectrum*.007f);
                    }
                }
            }
            mesh.Clear();mesh.SetVertices(points);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
            mesh.bounds=new Bounds(new Vector3(0,1.5f,2),new Vector3(10,5,12));renderer.enabled=ActiveFilaments>0;
        }
        public void Dispose(){Release(root);Release(mesh);Release(material);}
        static void Release(UnityEngine.Object o){if(!o)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
    }
}
