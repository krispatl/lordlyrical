using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    // A stone seal in the upper wall. Inner pieces release ahead of the shoulders.
    // Analytic trajectories allow cold seeks, restarts and identical network playback.
    public sealed class DragonBreach
    {
        readonly GameObject root;readonly Transform[] pieces=new Transform[48];
        readonly Vector3[] home=new Vector3[48];readonly Mesh[] meshes=new Mesh[48];
        readonly Material material;
        readonly GameObject intact;readonly Mesh intactMesh;
        public DragonBreach(Transform parent,Material stone)
        {
            root=new GameObject("Dragon breach | fractured stone seal");root.transform.SetParent(parent,false);root.transform.localPosition=Vector3.forward*12.45f;
            material=stone?new Material(stone):new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",new Color(.22f,.26f,.32f));
            for(int ring=0;ring<3;ring++)for(int seg=0;seg<16;seg++){
                int i=ring*16+seg;float a=seg*Mathf.PI/8+.008f,b=(seg+1)*Mathf.PI/8-.008f;
                float inner=ring/3f+.006f,outer=(ring+1)/3f-.006f;
                Vector3[] p={Point(a,inner),Point(b,inner),Point(b,outer),Point(a,outer)};
                var center=(p[0]+p[1]+p[2]+p[3])*.25f;home[i]=center+new Vector3(0,6.8f,14.35f);
                var v=new Vector3[8];var uv=new Vector2[8];for(int k=0;k<8;k++){v[k]=p[k%4]-center+Vector3.forward*(k<4?-.24f:.24f);uv[k]=new Vector2(p[k%4].x,p[k%4].y)*.3f;}
                var mesh=new Mesh{name="Breach stone "+i};mesh.vertices=v;mesh.uv=uv;
                mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[i]=mesh;
                var go=new GameObject("Breach stone "+i);go.transform.SetParent(root.transform,false);pieces[i]=go.transform;go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            }
            // One draw for the intact seal; separate moving shards only during the breach.
            var combine=new CombineInstance[48];for(int i=0;i<48;i++)combine[i]=new CombineInstance{mesh=meshes[i],transform=Matrix4x4.Translate(home[i])};
            intactMesh=new Mesh{name="Intact breach seal"};intactMesh.CombineMeshes(combine,true,true);
            intact=new GameObject("Intact stone seal");intact.transform.SetParent(root.transform,false);intact.AddComponent<MeshFilter>().sharedMesh=intactMesh;
            var intactRenderer=intact.AddComponent<MeshRenderer>();intactRenderer.sharedMaterial=material;intactRenderer.shadowCastingMode=ShadowCastingMode.Off;intactRenderer.receiveShadows=false;
        }
        static Vector3 Point(float a,float r)=>new Vector3(Mathf.Cos(a)*5.7f*r,Mathf.Sin(a)*4.0f*r,0);
        public void Render(float t,bool showing)
        {
            root.SetActive(showing&&t>=63.16f&&t<69);
            if(!root.activeSelf)return;
            // The existing temple is the housing; never overlay an intact stone disc.
            intact.SetActive(false);
            if(t<62.9f){foreach(var piece in pieces)piece.gameObject.SetActive(false);return;}
            for(int i=0;i<pieces.Length;i++){
                int ring=i/16;float release=ring==0?63.16f:ring==1?64.28f:DragonRevealScore.Wings;
                float age=Mathf.Max(0,t-release-(i%4)*.025f);float a=(i%16+.5f)*Mathf.PI/8;
                var velocity=new Vector3(Mathf.Cos(a)*(1.8f+ring*.6f),Mathf.Sin(a)*1.2f+1.1f,-.6f-ring*.15f);
                float shake=t<release?BattleScore.W(62.8f,63,release-.05f,release,t)*Mathf.Sin(t*63+i)*.025f:0;
                pieces[i].localPosition=home[i]+velocity*age+Vector3.down*(age*age*1.6f)+Vector3.forward*shake;
                pieces[i].localRotation=Quaternion.Euler(age*(29+i%5*13),age*(i%2==0?41:-37),age*(i%3-1)*62);
                pieces[i].localScale=Vector3.one*(1-DragonRevealScore.S(1.55f,2.6f,age));
                pieces[i].gameObject.SetActive(t>=release+(i%4)*.025f&&age<2.6f);
            }
        }
        public void Dispose(){foreach(var mesh in meshes)Kill(mesh);Kill(intactMesh);Kill(material);Kill(root);}
        static void Kill(Object o){if(!o)return;if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
    }
}
