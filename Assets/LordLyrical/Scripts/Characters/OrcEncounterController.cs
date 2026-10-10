using UnityEngine;
using LordLyrical.Characters;
namespace LordLyrical
{
    public sealed class OrcEncounterController
    {
        readonly Transform[] orcs=new Transform[12],clubs=new Transform[12],heads42=new Transform[12];readonly Vector3[] origins=new Vector3[12];
        readonly Material[] materials=new Material[12];readonly TextMesh[] indicators=new TextMesh[12];
        readonly SonicOrcPerformance[] performances=new SonicOrcPerformance[12];
        readonly OrcAttackVisual[] combatVisuals=new OrcAttackVisual[12]; readonly Quaternion[] gazeBase42=new Quaternion[12];readonly bool[] gazeApplied42=new bool[12];
        public Vector3 Target(int index)=>orcs[Mathf.Clamp(index,0,11)].position+Vector3.up*1.65f;
        public void RenderInteractive(ConcertState state,float t,Vector3[] targets=null){
            for(int i=0;i<12;i++){
                var o=state.orcs[i];float age=Mathf.Max(0,t-(o.health<=0?o.defeatedAt:o.entered));bool alive=o.health>0;
                bool visible=t>=26+i/8*.4f&&t<54&&(alive||age<3.3f);orcs[i].gameObject.SetActive(visible);if(!visible)continue;
                float prediction=Mathf.Clamp(t-state.time,0,.18f),smooth=Application.isPlaying?1-Mathf.Exp(-Time.deltaTime*18):1;
                Vector3 recoil=ImpactReaction37.Offset(t,o)+Presentation43Score.DeathDirection(o)*Presentation43Score.DeathLean(t,o)*.12f;
                Vector3 target=o.position+o.velocity*prediction+recoil;
                orcs[i].position=Vector3.Distance(orcs[i].position,target)>4?target:Vector3.Lerp(orcs[i].position,target,smooth);orcs[i].rotation=Quaternion.Slerp(orcs[i].rotation,Quaternion.Euler(0,o.yaw,0),smooth);
                bool attack=o.intent==OrcIntent.Telegraph||o.intent==OrcIntent.Strike||o.intent==OrcIntent.Recover;
                int clip=o.intent switch{OrcIntent.Pursuing or OrcIntent.Flanking=>1,OrcIntent.Stagger=>3,OrcIntent.Defeated=>4,OrcIntent.Retreat=>5,OrcIntent.Bracing=>10,_=>0};
                float travel=o.travelled+o.velocity.magnitude*prediction,clock=age;
                if(clip==1)clock=travel/1.8f;
                else if(clip==5)clock=travel/2.6f*(5f/6);
                else if(attack){clip=OrcCombatMotion.Clip(o.attack);clock=OrcCombatMotion.NormalizedTime(o.attack,t-o.attackStarted)*OrcCombatMotion.SourceDuration(o.attack);}
                if(heads42[i]&&gazeApplied42[i])heads42[i].localRotation=gazeBase42[i]; if(performances[i]){performances[i].MovementSpeed=o.velocity.magnitude;performances[i].SampleClip(clip,clock);performances[i].ApplyImpact(o,t);performances[i].SetResonance(o.intent==OrcIntent.Telegraph?3.5f:1.1f+(1-o.health/100)*.5f+Mathf.Exp(-Mathf.Max(0,t-o.lastHitTime)*12)*2,alive?0:Mathf.Clamp01((age-1.65f)/1.35f));}
                if(heads42[i]){gazeBase42[i]=heads42[i].localRotation;gazeApplied42[i]=true;} if(heads42[i]&&alive&&targets!=null&&o.target>=0&&o.target<targets.Length&&(o.intent==OrcIntent.Telegraph||o.intent==OrcIntent.Pursuing||o.intent==OrcIntent.Flanking)){
                    var d=targets[o.target]-heads42[i].position;float yaw=Mathf.Clamp(Mathf.DeltaAngle(o.yaw,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg),-20,20);float pitch=Mathf.Clamp(Mathf.Atan2(d.y,Mathf.Max(.01f,new Vector2(d.x,d.z).magnitude))*Mathf.Rad2Deg,-10,10);
                    heads42[i].rotation=Quaternion.AngleAxis(yaw,Vector3.up)*Quaternion.AngleAxis(-pitch,orcs[i].right)*heads42[i].rotation;
                }
                combatVisuals[i].Sample(o,t,performances[i]?performances[i].rightWeaponSocket:null);
                bool threat=o.intent==OrcIntent.Telegraph||o.intent==OrcIntent.Strike;
                indicators[i].gameObject.SetActive(threat||o.intent==OrcIntent.Stagger);
                indicators[i].text=threat?o.attack.ToString().ToUpperInvariant()+" · GUARD":Mathf.CeilToInt(o.health)+"%";indicators[i].color=threat?new Color(1,.33f,.08f):Color.cyan;
                if(Camera.main)indicators[i].transform.rotation=Quaternion.LookRotation(indicators[i].transform.position-Camera.main.transform.position);
            }
        }
        public OrcEncounterController(Transform parent,GameObject model,GameObject weapon=null)
        {
            var torso=ResonantGeometry.Lathe(new[]{0f,.35f,1.15f,1.9f,2.2f,2.35f},new[]{.4f,.6f,.5f,.88f,.55f,.1f},10);
            var skull=ResonantGeometry.Lathe(new[]{0f,.2f,.5f,.65f},new[]{.25f,.4f,.3f,.05f},7);
            var clubMesh=ResonantGeometry.Tube(new[]{Vector3.zero,Vector3.up*1.2f,new Vector3(.3f,1.2f,0),new Vector3(.3f,1.9f,0)},new[]{.08f,.08f,.1f,.09f},6);
            for(int i=0;i<12;i++)
            {
                origins[i]=new Vector3((i%4-1.5f)*2.8f,0,17+(i/4)*2.5f);
                var root=ResonantGeometry.Node("Sonic Orc "+(i+1),parent,origins[i]);orcs[i]=root;materials[i]=ResonantGeometry.Material("Orc plates "+i,new Color(.65f,.22f,.04f),.25f);
                combatVisuals[i]=root.gameObject.AddComponent<OrcAttackVisual>();
                if(model!=null)
                {
                    var m=Object.Instantiate(model,root);m.transform.localPosition=Vector3.zero;
                    performances[i]=m.GetComponent<SonicOrcPerformance>();foreach(var bone in m.GetComponentsInChildren<Transform>(true))if(bone.name=="head"||bone.name=="Head"||bone.name=="mixamorig:Head")heads42[i]=bone;
                    if(performances[i])
                    {
                        // The authored asset is already 2.9 m tall; animated culling bounds are larger.
                        m.transform.localScale=Vector3.one;performances[i].previewAutomatically=false;
                    }
                    else
                    {
                        var bounds=new Bounds(root.position,Vector3.zero);
                        foreach(var r in m.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
                        float s=2.9f/Mathf.Max(bounds.size.y,.01f);m.transform.localScale*=s;
                        m.transform.localPosition-=Vector3.up*bounds.min.y*s;
                    }
                }
                else
                {
                    ResonantGeometry.MeshObject("Resonance torso",root,torso,materials[i],Vector3.zero,Vector3.one);
                    ResonantGeometry.MeshObject("Tusked mask",root,skull,materials[i],Vector3.up*2.1f,Vector3.one);
                    for(int k=0;k<5;k++)ResonantGeometry.MeshObject("Overlapping plate",root,ResonantGeometry.Ring(.75f-k*.06f,.12f,8),materials[i],Vector3.up*(1.25f+k*.17f),new Vector3(1,1,.7f));
                    foreach(int side in new[]{-1,1}){var limb=ResonantGeometry.Tube(new[]{new Vector3(side*.62f,1.9f,0),new Vector3(side*.95f,1.3f,0),new Vector3(side*.75f,.7f,.2f)},new[]{.3f,.25f,.18f},8);ResonantGeometry.MeshObject("Heavy arm",root,limb,materials[i],Vector3.zero,Vector3.one);}
                }
                clubs[i]=ResonantGeometry.Node("Tuning fork club",root,new Vector3(-1,1.2f,.25f));
                if(weapon){var prop=Object.Instantiate(weapon,clubs[i]);prop.transform.localPosition=Vector3.zero;prop.transform.localRotation=Quaternion.identity;}
                else {
                ResonantGeometry.MeshObject("Fork",clubs[i],clubMesh,materials[i],Vector3.zero,Vector3.one).transform.localRotation=Quaternion.Euler(150,0,0);
                var other=ResonantGeometry.MeshObject("Second fork",clubs[i],clubMesh,materials[i],Vector3.zero,new Vector3(-1,1,1));other.transform.localRotation=Quaternion.Euler(150,0,0);
                }
                if(performances[i]&&performances[i].rightWeaponSocket)
                {
                    clubs[i].SetParent(performances[i].rightWeaponSocket,false);
                    clubs[i].localPosition=Vector3.zero;clubs[i].localRotation=Quaternion.identity;clubs[i].localScale=Vector3.one*(weapon?1f:.55f);
                    foreach(Transform fork in clubs[i])fork.localRotation=Quaternion.identity;
                }
                var label=ResonantGeometry.Node("Root Key D",root,Vector3.up*3.3f).gameObject.AddComponent<TextMesh>();label.text="D";label.anchor=TextAnchor.MiddleCenter;label.characterSize=.045f;label.fontSize=48;label.color=RootKeys.Slots[0].color;indicators[i]=label;label.transform.localRotation=Quaternion.Euler(0,180,0);
            }
        }
        public void Render(ShowFrame frame)
        {
            float t=(float)frame.time;
            for(int i=0;i<12;i++)
            {
                float emerge=26+(i>=8?.7f:0),dissolve=i==0?Mathf.Clamp01((t-39)*1.4f):i>=9?Mathf.Clamp01((t-47.4f)/.9f):0;
                bool dropped=i>=9&&t>=45;
                orcs[i].gameObject.SetActive(t>=emerge&&t<54&&dissolve<1);
                float advance=Mathf.Clamp(t-26,0,21)*.14f,retreat=t>=50&&!dropped?(t-50)*4:0;
                float y=dropped?Mathf.SmoothStep(0,.75f,Mathf.Clamp01((t-45)/1.2f)):Mathf.Max(0,1-(t-emerge))*-3;
                float stagger=t>=39&&t<41?Mathf.Sin((t-39)*18)*.13f:0;
                orcs[i].localPosition=origins[i]+new Vector3(stagger,y,-advance+retreat);
                orcs[i].localRotation=Quaternion.Euler(dropped?85*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-45)/1.2f)):t>=47&&t<48?Mathf.Sin((t-47)*Mathf.PI)*16:0,t>=50?180:180+stagger*40,0);
                if(performances[i])
                {
                    if(orcs[i].gameObject.activeInHierarchy)performances[i].SampleEncounter(t,i,frame.deepSonar,dissolve);
                }
                else clubs[i].localRotation=Quaternion.Euler(t>=47&&t<48?-Mathf.Sin((t-47)*Mathf.PI)*100:Mathf.Sin(t*3+i)*12,0,0);
                materials[i].SetFloat("_Dissolve",dissolve);materials[i].SetFloat("_Phase",t*2);materials[i].SetFloat("_Glow",frame.deepSonar?2:.22f);
                indicators[i].gameObject.SetActive(t>=31+i*.07f&&t<39);
            }
        }
    }
}
