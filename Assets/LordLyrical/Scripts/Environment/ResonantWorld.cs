using UnityEngine;
namespace LordLyrical
{
    public class ResonantWorld:MonoBehaviour
    {
        ExperienceConfiguration config;NetworkSessionController session;Transform chamber,vortex,well;CueTimeline timing;
        Material stone,floor,sourceMaterial;Transform[] columns=new Transform[12],vortexRings=new Transform[36];
        Material[] vortexMaterials=new Material[12];RoleVisual[] roles=new RoleVisual[10];
        OrcEncounterController orcs;DragonEncounterController dragon;LordLyricalController lord;VFXDirector effects;
        TextMesh title,call;Transform glyph;Material glyphMaterial;bool built;
        public void Initialize(ExperienceConfiguration configuration,NetworkSessionController network)
        {
            if(built)return;built=true;config=configuration;session=network;
            timing=new CueTimeline(JsonUtility.FromJson<CueTable>(config.cueJson.text).cues);
            chamber=ResonantGeometry.Node("Chamber of the Root",transform,Vector3.zero);
            if(config.useSourceChamber){var sourceMesh=Resources.Load<Mesh>("ReferenceChamber");if(sourceMesh!=null){sourceMaterial=new Material(Shader.Find("LordLyrical/ChamberSurfel"));ResonantGeometry.MeshObject("Supplied vaulted chamber",chamber,sourceMesh,sourceMaterial,Vector3.zero,Vector3.one);}}
            vortex=ResonantGeometry.Node("Frequency Vortex",transform,Vector3.zero);
            stone=ResonantGeometry.Material("Obsidian cymatic walls",new Color(.15f,.4f,.55f),.12f);stone.SetFloat("_Frequency",2);
            floor=ResonantGeometry.Material("Polished obsidian floor",new Color(.22f,.35f,.55f),.12f);floor.SetFloat("_Frequency",3);
            stone.SetFloat("_Environment",1);floor.SetFloat("_Environment",1);
            ResonantGeometry.MeshObject("Cymatic obsidian floor",chamber,ResonantGeometry.Ring(10,20,128),floor,new Vector3(0,-.02f,3),Vector3.one);
            var column=ResonantGeometry.Lathe(new[]{0f,.3f,.5f,1f,6.5f,7f,7.5f,8f},new[]{.8f,.8f,.6f,.45f,.45f,.6f,.9f,.8f},10);
            var wallMesh=ResonantGeometry.Lathe(new[]{0f,3f,7f,10f},new[]{17f,17f,15f,10f},32);
            ResonantGeometry.MeshObject("Carved chamber enclosure",chamber,wallMesh,stone,new Vector3(0,0,3),Vector3.one);
            for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;columns[i]=ResonantGeometry.MeshObject("Resonance column "+i,chamber,column,stone,new Vector3(Mathf.Sin(a)*12,0,3+Mathf.Cos(a)*12),Vector3.one).transform;}
            var wellMaterial=ResonantGeometry.Material("Frequency Well mouth",new Color(.1f,.8f,.8f),.7f);
            well=ResonantGeometry.Node("Breathing Frequency Well",transform,new Vector3(0,0,10));
            for(int i=0;i<6;i++)ResonantGeometry.MeshObject("Well carved lip",well,ResonantGeometry.Ring(1.8f+i*.2f,.16f,64),wellMaterial,new Vector3(0,.015f+i*.018f,0),Vector3.one);
            for(int i=0;i<12;i++){vortexMaterials[i]=ResonantGeometry.Material("Vortex spectrum "+i,RootKeys.Spectrum(i/12f),1.5f);vortexMaterials[i].SetFloat("_Frequency",4+i%4);}
            var ringMesh=ResonantGeometry.Ring(7,.08f,72);
            for(int i=0;i<36;i++){vortexRings[i]=ResonantGeometry.MeshObject("Frequency tunnel band "+i,vortex,ringMesh,vortexMaterials[i%12],new Vector3(0,2,i*2),Vector3.one).transform;vortexRings[i].localRotation=Quaternion.Euler(90,0,0);}
            for(int i=0;i<10;i++)roles[i]=new RoleVisual(transform,i);
            orcs=new OrcEncounterController(chamber,config.orcModel);dragon=new DragonEncounterController(chamber);lord=new LordLyricalController(transform,config.lordPortrait);
            effects=new VFXDirector(transform,JsonUtility.FromJson<CueTable>(config.cueJson.text).cues);
            var tt=ResonantGeometry.Node("Cymatic title",transform,new Vector3(0,4.3f,9));tt.localRotation=Quaternion.identity;title=tt.gameObject.AddComponent<TextMesh>();
            title.text="LORD LYRICAL\nTHE RESONANT REALM\n\nA VR Experience\n\nCreated by Jeru The Damaja\n5th Yang LLC\nMaximum Warp Entertainment";
            title.anchor=TextAnchor.MiddleCenter;title.alignment=TextAlignment.Center;title.fontSize=64;title.characterSize=.22f;title.lineSpacing=1.1f;title.color=new Color(.65f,.92f,1);
            call=ResonantGeometry.Node("Spatial call fallback",transform,new Vector3(0,3.2f,8)).gameObject.AddComponent<TextMesh>();call.anchor=TextAnchor.MiddleCenter;call.alignment=TextAlignment.Center;call.fontSize=64;call.characterSize=.17f;call.color=Color.cyan;
            glyphMaterial=ResonantGeometry.Material("Counter Key A",RootKeys.Slots[9].color,2);
            glyph=ResonantGeometry.MeshObject("Counter key glyph",chamber,ResonantGeometry.Ring(1,.06f,6),glyphMaterial,new Vector3(0,4,15),Vector3.one*2).transform;glyph.localRotation=Quaternion.Euler(90,0,0);
        }
        public void ResetEffects(){if(built)effects.Reset();}
        public void Render(ShowFrame frame,bool showing)
        {
            if(!built)return;double actual=frame.time;frame=ShowFrame.At(timing.NarrativeTime(actual));frame.musicTime=actual;float t=(float)frame.time;Shader.SetGlobalFloat("_ShowTime",t);
            Shader.SetGlobalColor("_ParticipantTint",session.Slot>=0?RootKeys.Slots[session.Slot].color:Color.white);
            chamber.gameObject.SetActive(!frame.vortex);vortex.gameObject.SetActive(frame.vortex);
            well.localPosition=frame.vortex?new Vector3(0,-3,5):new Vector3(0,0,10);well.localScale=Vector3.one*(1+Mathf.Sin(t*.8f)*(frame.vortex?.08f:.025f));
            float glow=frame.art?1.4f:frame.wallActivation>0?2:frame.deepSonar?1.8f:t>=96?Mathf.Lerp(.12f,1,(t-96)/2):.12f;
            if(t>=111)glow=Mathf.Lerp(1.4f,.25f,Mathf.Clamp01((t-111)/6));
            if(sourceMaterial!=null){sourceMaterial.SetFloat("_Glow",glow);sourceMaterial.SetFloat("_Evolution",frame.art?1:0);}
            stone.SetFloat("_Glow",showing?glow:.04f);stone.SetFloat("_Evolution",frame.art?1:0);stone.SetFloat("_Phase",frame.art&&t<111?0:t*.12f);stone.SetFloat("_Cracks",frame.dragonFractured?1:0);
            float cracks=frame.fissured||frame.dragonFractured&&!frame.art?1:t>=52&&t<55?1-(t-52)/3:0;
            floor.SetFloat("_Cracks",cracks);floor.SetFloat("_Glow",t>=111?glow:frame.art?1.2f:.2f);floor.SetFloat("_Evolution",frame.art?1:0);floor.SetFloat("_Phase",frame.art?0:t*.1f);
            for(int i=0;i<12;i++){columns[i].localRotation=Quaternion.Euler(0,0,i<3&&frame.dragonFractured?6:0);vortexMaterials[i].SetFloat("_Phase",t*.5f+i);}
            for(int i=0;i<36;i++){var r=vortexRings[i];r.gameObject.SetActive(config.vfxQuality>0||i%2==0);float z=Mathf.Repeat(i*2-t*7,72)-8;r.localPosition=new Vector3(Mathf.Sin(i*.4f+t*.12f)*.5f,2+Mathf.Cos(i*.3f)*.5f,z);r.localRotation=Quaternion.Euler(90,0,i*13+t*9);float s=(1+Mathf.Sin(i*.4f+t*.3f)*.08f)*(1-.65f*Mathf.Clamp01(t-20));r.localScale=Vector3.one*s;}
            for(int i=0;i<10;i++){bool occupied=false;foreach(var p in session.Participants.participants)if(p.slot==i)occupied=p.connected;roles[i].Render(frame,occupied,i==session.Slot);}
            orcs.Render(frame);dragon.Render(frame);lord.Render(frame);
            effects.Render(frame,dragon.Mouth,dragon.Heart,lord.root.position);
            title.gameObject.SetActive(frame.title);title.transform.localScale=Vector3.one*Mathf.Lerp(.65f,1,frame.titleProgress);title.color=new Color(.6f+.35f*frame.titleProgress,.88f,1,frame.titleProgress);
            glyph.gameObject.SetActive(t>=34&&t<39);glyph.localRotation=Quaternion.Euler(90,0,t*8);
            bool counter=t>=34&&t<39,signal=t>=83&&t<87,identify=t>=87&&t<90,rise=t>=92&&t<96;
            call.gameObject.SetActive(counter||signal||identify||rise);
            string words=counter?"KEY OF A\nCantors to the front\nFollow the line":signal?"ROOT KEY E\nThe Blade":identify?"E beneath Bb\nThe Discordant":"ALL TEN\nOne frequency";
            if(call.text!=words)call.text=words;call.color=counter?RootKeys.Slots[9].color:signal?RootKeys.Slots[2].color:identify?RootKeys.Slots[8].color:Color.white;
        }
    }
}

