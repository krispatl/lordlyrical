using UnityEngine;
namespace LordLyrical
{
    public sealed class DragonEncounterController
    {
        public Transform root,head,leftWing,rightWing;readonly Material shell,core,eyes;readonly Transform[] ribs=new Transform[16];
        public Vector3 Mouth=>head.position+new Vector3(0,-.3f,-.9f);
        public Vector3 Heart=>root.position+new Vector3(0,0,-1);
        public DragonEncounterController(Transform parent)
        {
            root=ResonantGeometry.Node("Sound Dragon - living architecture",parent,new Vector3(0,8,11));
            shell=ResonantGeometry.Material("Corrupted frequency scales",new Color(.42f,.07f,.17f),.1f);
            core=ResonantGeometry.Material("Original E signature",RootKeys.Slots[2].color,2);
            eyes=ResonantGeometry.Material("Cycling eyes",Color.red,3);
            var body=ResonantGeometry.Tube(new[]{new Vector3(0,0,-3),new Vector3(0,.3f,0),new Vector3(0,-.6f,3),new Vector3(1,-2,6),new Vector3(3,-5,7)},new[]{1.4f,2.2f,1.5f,.8f,.02f},20);
            ResonantGeometry.MeshObject("Wave body",root,body,shell,Vector3.zero,Vector3.one);
            ResonantGeometry.MeshObject("Hidden E core",root,ResonantGeometry.Lathe(new[]{-1.2f,0f,1.2f},new[]{0f,.75f,0f},12),core,new Vector3(0,-.4f,-1),Vector3.one);
            head=ResonantGeometry.Node("Dragon head",root,new Vector3(0,-1,-3.5f));
            var muzzle=ResonantGeometry.Tube(new[]{new Vector3(0,.6f,.5f),new Vector3(0,0,-.4f),new Vector3(0,-.3f,-1.5f)},new[]{1.1f,.85f,.35f},8);
            ResonantGeometry.MeshObject("Frequency skull",head,muzzle,shell,Vector3.zero,Vector3.one);
            for(int side=-1;side<=1;side+=2)
            {
                ResonantGeometry.MeshObject("Eye",head,ResonantGeometry.Lathe(new[]{0f,.3f,.6f},new[]{0f,.18f,0f},6),eyes,new Vector3(side*.66f,.15f,-.65f),Vector3.one).transform.localRotation=Quaternion.Euler(90,0,0);
                ResonantGeometry.MeshObject("Horn",head,ResonantGeometry.Tube(new[]{new Vector3(side*.6f,.5f,0),new Vector3(side*1.2f,1.5f,.5f),new Vector3(side*.9f,2.4f,1.5f)},new[]{.3f,.17f,0f},8),shell,Vector3.zero,Vector3.one);
                var wing=ResonantGeometry.Node(side<0?"Wall wing left":"Wall wing right",root,new Vector3(side*1.5f,0,0));if(side<0)leftWing=wing;else rightWing=wing;
                for(int k=0;k<8;k++)
                {
                    var points=new[]{Vector3.zero,new Vector3(side*4,1.2f,k*.15f),new Vector3(side*(10-k*.45f),-.5f,k*.7f-3),new Vector3(side*(12-k*.65f),-5,k*.95f-4)};
                    ribs[(side<0?0:8)+k]=ResonantGeometry.MeshObject("Architectural wing rib "+k,wing,ResonantGeometry.Tube(points,new[]{.3f,.22f,.15f,.015f},8),shell,Vector3.zero,Vector3.one).transform;
                    if(k<7){var vertices=new[]{points[1],points[2],points[3],points[3]+new Vector3(-side*.65f,0,.95f)};var mesh=new Mesh{vertices=vertices,triangles=new[]{0,1,2,0,2,3},uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up}};mesh.RecalculateNormals();ResonantGeometry.MeshObject("Frequency membrane",wing,mesh,shell,Vector3.zero,Vector3.one);}
                }
            }
            for(int k=0;k<10;k++)ResonantGeometry.MeshObject("Compressed air scale",root,ResonantGeometry.Ring(1.4f+Mathf.Sin(k*.3f)*.6f,.13f,28),shell,new Vector3(0,0,k*.55f-2.5f),Vector3.one).transform.localRotation=Quaternion.Euler(90,0,0);
        }
        public void Render(ShowFrame frame)
        {
            float t=(float)frame.time,reveal=frame.dragonReveal;root.gameObject.SetActive(t>=63&&t<101);
            root.localPosition=new Vector3(0,Mathf.Lerp(10,7,reveal)-frame.dragonDissolve*6,11+frame.dragonDissolve*2);
            float flap=t>=69&&t<96?Mathf.Sin((t-69)*1.8f)*6:0;
            if(t>=90&&t<92)flap=Mathf.Sin(t*32)*4;
            leftWing.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(62,0,reveal)+flap);
            rightWing.localRotation=Quaternion.Euler(0,0,-Mathf.Lerp(62,0,reveal)-flap);
            head.localRotation=Quaternion.Euler(reveal*12, t>=69&&t<81?Mathf.Sin(t*.65f)*22:0,0);
            shell.SetFloat("_Glow",t>=98?4:frame.wallActivation>0?1.8f:t>=63?.45f:.03f);
            shell.SetFloat("_Phase",t>=96&&t<98?0:t*.5f);shell.SetFloat("_Dissolve",t>=98?frame.dragonDissolve:frame.deepSonar?.82f:t>=90?.4f:0);
            shell.SetFloat("_Cracks",t>=90?1:0);shell.SetFloat("_Evolution",t>=98?1:0);
            core.SetFloat("_Glow",t>=90?4:frame.deepSonar?1.6f:0);core.SetFloat("_Dissolve",frame.dragonDissolve);
            int eye=(int)(t*2)%3;eyes.SetColor("_Color",eye==0?new Color(1,.35f,.01f):eye==1?Color.red:Color.black);
            eyes.SetFloat("_Glow",t>=65.152?3:0);
        }
    }
}
