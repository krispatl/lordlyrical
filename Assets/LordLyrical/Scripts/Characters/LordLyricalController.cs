using UnityEngine;
namespace LordLyrical
{
    public sealed class LordLyricalController
    {
        public Transform root,leftHand,rightHand;readonly Material material;readonly Transform compass;
        public LordLyricalController(Transform parent,Texture2D portrait)
        {
            root=ResonantGeometry.Node("Lord Lyrical - eleventh presence",parent,new Vector3(0,0,5));material=ResonantGeometry.Material("Lord Lyrical full spectrum",Color.cyan,1);
            ResonantGeometry.MeshObject("Founder mantle",root,ResonantGeometry.Lathe(new[]{0f,.2f,1f,1.6f,1.85f},new[]{.4f,.45f,.26f,.6f,.14f},12),material,Vector3.zero,Vector3.one);
            var portraitMesh=new Mesh{vertices=new[]{new Vector3(-.45f,0,0),new Vector3(.45f,0,0),new Vector3(-.45f,.95f,0),new Vector3(.45f,.95f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};portraitMesh.RecalculateNormals();
            if(portrait!=null){var pm=new Material(Shader.Find("LordLyrical/Wave"));pm.SetTexture("_MainTex",portrait);pm.SetColor("_Color",Color.white);ResonantGeometry.MeshObject("Jeru source portrait hologram",root,portraitMesh,pm,new Vector3(0,1.3f,-.12f),Vector3.one);}
            for(int side=-1;side<=1;side+=2){var hand=ResonantGeometry.MeshObject("Spectrum hand",root,ResonantGeometry.Lathe(new[]{0f,.1f,.25f},new[]{.06f,.14f,.02f},8),material,new Vector3(side*.75f,1.4f,0),Vector3.one).transform;if(side<0)leftHand=hand;else rightHand=hand;}
            compass=ResonantGeometry.Node("Twelve frequency compass",root,Vector3.up*2.6f);
            for(int i=0;i<12;i++){var cm=ResonantGeometry.Material("Spectrum "+i,RootKeys.Spectrum(i/12f),2);var r=ResonantGeometry.MeshObject("Spectrum orbit",compass,ResonantGeometry.Ring(.7f+i*.04f,.018f,64),cm,Vector3.zero,Vector3.one);r.transform.localRotation=Quaternion.Euler(i*15,0,i*15);}
        }
        public void Render(ShowFrame frame)
        {
            float t=(float)frame.time;bool circle=t>=69&&t<90;
            root.localPosition=circle?new Vector3(Mathf.Sin((t-69)*.5f)*3,0,5+Mathf.Cos((t-69)*.5f)):new Vector3(0,0,5);
            float cycle=t>=81?Mathf.Floor(t*2)/12:t>=59&&t<61?t*.7f:t*.04f;
            material.SetColor("_Color",RootKeys.Spectrum(cycle));material.SetFloat("_Glow",t<21?.3f:t>=92?3:1);
            float raised=t>=92?Mathf.Clamp01((t-92)/4):t>=59&&t<61?1:t>=81&&t<83?1:0;
            leftHand.localPosition=new Vector3(-.75f,1.4f+raised*.9f,-raised*.3f);rightHand.localPosition=new Vector3(.75f,1.4f+raised*.9f,-raised*.3f);
            compass.localRotation=Quaternion.Euler(0,t*15,0);compass.gameObject.SetActive(t>=21);
            if(t>=87&&t<92)root.localRotation=Quaternion.Euler(0,-45,0);else root.localRotation=Quaternion.identity;
        }
    }
}
