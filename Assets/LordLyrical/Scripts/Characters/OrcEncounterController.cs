using UnityEngine;
namespace LordLyrical
{
    public sealed class OrcEncounterController
    {
        readonly Transform[] orcs=new Transform[12],clubs=new Transform[12];readonly Vector3[] origins=new Vector3[12];
        readonly Material[] materials=new Material[12];readonly TextMesh[] indicators=new TextMesh[12];
        public OrcEncounterController(Transform parent,GameObject model)
        {
            var torso=ResonantGeometry.Lathe(new[]{0f,.35f,1.15f,1.9f,2.2f,2.35f},new[]{.4f,.6f,.5f,.88f,.55f,.1f},10);
            var skull=ResonantGeometry.Lathe(new[]{0f,.2f,.5f,.65f},new[]{.25f,.4f,.3f,.05f},7);
            var clubMesh=ResonantGeometry.Tube(new[]{Vector3.zero,Vector3.up*1.2f,new Vector3(.3f,1.2f,0),new Vector3(.3f,1.9f,0)},new[]{.08f,.08f,.1f,.09f},6);
            for(int i=0;i<12;i++)
            {
                origins[i]=new Vector3((i%4-1.5f)*2.8f,0,17+(i/4)*2.5f);
                var root=ResonantGeometry.Node("Sonic Orc "+(i+1),parent,origins[i]);orcs[i]=root;materials[i]=ResonantGeometry.Material("Orc plates "+i,new Color(.65f,.22f,.04f),.25f);
                if(model!=null)
                {var m=Object.Instantiate(model,root);m.transform.localPosition=Vector3.zero;var bounds=new Bounds(root.position,Vector3.zero);foreach(var r in m.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);float s=2.9f/Mathf.Max(bounds.size.y,.01f);m.transform.localScale*=s;m.transform.localPosition-=Vector3.up*bounds.min.y*s;foreach(var r in m.GetComponentsInChildren<Renderer>())r.sharedMaterial=materials[i];}
                else
                {
                    ResonantGeometry.MeshObject("Resonance torso",root,torso,materials[i],Vector3.zero,Vector3.one);
                    ResonantGeometry.MeshObject("Tusked mask",root,skull,materials[i],Vector3.up*2.1f,Vector3.one);
                    for(int k=0;k<5;k++)ResonantGeometry.MeshObject("Overlapping plate",root,ResonantGeometry.Ring(.75f-k*.06f,.12f,8),materials[i],Vector3.up*(1.25f+k*.17f),new Vector3(1,1,.7f));
                    foreach(int side in new[]{-1,1}){var limb=ResonantGeometry.Tube(new[]{new Vector3(side*.62f,1.9f,0),new Vector3(side*.95f,1.3f,0),new Vector3(side*.75f,.7f,.2f)},new[]{.3f,.25f,.18f},8);ResonantGeometry.MeshObject("Heavy arm",root,limb,materials[i],Vector3.zero,Vector3.one);}
                }
                clubs[i]=ResonantGeometry.Node("Tuning fork club",root,new Vector3(-1,1.2f,.25f));
                ResonantGeometry.MeshObject("Fork",clubs[i],clubMesh,materials[i],Vector3.zero,Vector3.one).transform.localRotation=Quaternion.Euler(150,0,0);
                var other=ResonantGeometry.MeshObject("Second fork",clubs[i],clubMesh,materials[i],Vector3.zero,new Vector3(-1,1,1));other.transform.localRotation=Quaternion.Euler(150,0,0);
                var label=ResonantGeometry.Node("Root Key D",root,Vector3.up*3.3f).gameObject.AddComponent<TextMesh>();label.text="D";label.anchor=TextAnchor.MiddleCenter;label.characterSize=.3f;label.fontSize=48;label.color=RootKeys.Slots[0].color;indicators[i]=label;label.transform.localRotation=Quaternion.Euler(0,180,0);
            }
        }
        public void Render(ShowFrame frame)
        {
            float t=(float)frame.time;
            for(int i=0;i<12;i++)
            {
                float emerge=26+(i>=8?.7f:0),dissolve=i==0?Mathf.Clamp01((t-39)*1.4f):0;
                bool dropped=i>=9&&t>=45;
                orcs[i].gameObject.SetActive(t>=emerge&&t<54&&dissolve<1);
                float advance=Mathf.Clamp(t-26,0,21)*.27f,retreat=t>=50?(t-50)*4:0;
                float y=dropped?-Mathf.Clamp01((t-45)*2)*1.2f:Mathf.Max(0,1-(t-emerge))*-3;
                float stagger=t>=39&&t<41?Mathf.Sin((t-39)*18)*.13f:0;
                orcs[i].localPosition=origins[i]+new Vector3(stagger,y,-advance+retreat);
                orcs[i].localRotation=Quaternion.Euler(dropped?85:t>=47&&t<48?Mathf.Sin((t-47)*Mathf.PI)*16:0,t>=50?180:180+stagger*40,0);
                clubs[i].localRotation=Quaternion.Euler(t>=47&&t<48?-Mathf.Sin((t-47)*Mathf.PI)*100:Mathf.Sin(t*3+i)*12,0,0);
                materials[i].SetFloat("_Dissolve",dissolve);materials[i].SetFloat("_Phase",t*2);materials[i].SetFloat("_Glow",frame.deepSonar?2:.22f);
                indicators[i].gameObject.SetActive(t>=31+i*.07f&&t<39);
            }
        }
    }
}
