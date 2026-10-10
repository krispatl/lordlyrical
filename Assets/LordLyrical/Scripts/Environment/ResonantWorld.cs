using UnityEngine;
namespace LordLyrical
{
    public class ResonantWorld:MonoBehaviour
    {
        ExperienceConfiguration config;NetworkSessionController session;Transform chamber,vortex,well;CueTimeline timing;
        Material stone,floor,sourceMaterial;Transform[] columns=new Transform[12],vortexRings=new Transform[36];
        Material[] vortexMaterials=new Material[12];RoleVisual[] roles=new RoleVisual[10];
        OrcEncounterController orcs;LordLyricalController lord;VFXDirector effects;
        DragonFireballVFX dragonFireballs;StoryPresentation40 story;StageMagic42 stageMagic;SurfaceEcho43 surfaceEcho;
        ChamberPresentation chamberArt;ShowArtPresentation finishing;
        TextMesh title,call;Transform glyph;Material glyphMaterial;bool built;
        MusicReactiveSystem music;ConcertPresentation concert;ConcertInteraction interaction;CharacterGroundContact groundContact;
        public Transform FounderRoot=>lord?.root;
        public void Initialize(ExperienceConfiguration configuration,NetworkSessionController network)
        {
            Shader.SetGlobalFloat("_RealmEntrance41",1);
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
            for(int i=0;i<10;i++)roles[i]=new RoleVisual(transform,i,config.participantModels!=null&&i<config.participantModels.Length?config.participantModels[i]:null);
            orcs=new OrcEncounterController(chamber,config.orcModel,config.orcWeapon);lord=new LordLyricalController(transform,config.lordPortrait,config.lordModel);
            music=new MusicReactiveSystem();concert=new GameObject("Living musical architecture").AddComponent<ConcertPresentation>();concert.transform.SetParent(transform,false);concert.Initialize(config.vfxQuality);
            interaction=gameObject.AddComponent<ConcertInteraction>();interaction.Initialize(session,orcs);
            if(config.finishingModel){finishing=Instantiate(config.finishingModel,transform).GetComponent<ShowArtPresentation>();finishing.Initialize(roles,orcs,lord,config.vfxQuality);}
            else effects=new VFXDirector(transform,JsonUtility.FromJson<CueTable>(config.cueJson.text).cues);
            var tt=ResonantGeometry.Node("Cymatic title",transform,new Vector3(0,4.3f,9));tt.localRotation=Quaternion.identity;title=tt.gameObject.AddComponent<TextMesh>();
            title.text="LORD LYRICAL\nTHE RESONANT REALM\n\nA VR Experience\n\nCreated by Jeru The Damaja\n5th Yang LLC\nMaximum Warp Entertainment";
            title.anchor=TextAnchor.MiddleCenter;title.alignment=TextAlignment.Center;title.fontSize=64;title.characterSize=.22f;title.lineSpacing=1.1f;title.color=new Color(.65f,.92f,1);
            call=ResonantGeometry.Node("Spatial call fallback",transform,new Vector3(0,3.2f,8)).gameObject.AddComponent<TextMesh>();call.anchor=TextAnchor.MiddleCenter;call.alignment=TextAlignment.Center;call.fontSize=64;call.characterSize=.014f;call.color=Color.cyan;
            glyphMaterial=ResonantGeometry.Material("Counter Key A",RootKeys.Slots[9].color,2);
            glyph=ResonantGeometry.MeshObject("Counter key glyph",chamber,ResonantGeometry.Ring(1,.06f,6),glyphMaterial,new Vector3(0,4,15),Vector3.one*2).transform;glyph.localRotation=Quaternion.Euler(90,0,0);
            if(config.chamberModel!=null)
            {
                var authored=Instantiate(config.chamberModel,chamber);
                authored.name="Authored Chamber of the Root";
                chamberArt=authored.GetComponent<ChamberPresentation>();
                if(chamberArt!=null)
                {
                    chamberArt.previewInEditor=false;chamberArt.Initialize();
                    chamber.Find("Cymatic obsidian floor").gameObject.SetActive(false);
                    chamber.Find("Carved chamber enclosure").gameObject.SetActive(false);
                    foreach(var columnTransform in columns)columnTransform.gameObject.SetActive(false);
                }
            }
            dragonFireballs=new GameObject("Dragon fireballs").AddComponent<DragonFireballVFX>();dragonFireballs.transform.SetParent(transform,false);dragonFireballs.Initialize();
            var contactObject=new GameObject("Character floor contact");
            contactObject.transform.SetParent(transform,false);
            groundContact=contactObject.AddComponent<CharacterGroundContact>();groundContact.Initialize(transform);story=gameObject.AddComponent<StoryPresentation40>();story.Initialize(session,interaction.Encounter);stageMagic=gameObject.AddComponent<StageMagic42>();stageMagic.Initialize(interaction.Encounter,chamberArt,this);surfaceEcho=gameObject.AddComponent<SurfaceEcho43>();surfaceEcho.Initialize(interaction.Encounter,GetComponent<ManualResonanceBeams>());
        }
        bool openingStateSet,lastOpeningIsolated,lastFounderVisible;
        Renderer[] openingRenderers;System.Collections.Generic.HashSet<Renderer> founderRenderers;
        public void SetOpeningPresentation(bool isolate,bool founderVisible,float songTime)
        {
            if(!built)return;
            if(openingRenderers==null){openingRenderers=GetComponentsInChildren<Renderer>(true);founderRenderers=new System.Collections.Generic.HashSet<Renderer>(lord.root.GetComponentsInChildren<Renderer>(true));}
            if(!openingStateSet||isolate!=lastOpeningIsolated||founderVisible!=lastFounderVisible){
                foreach(var r in openingRenderers)if(r)r.forceRenderingOff=isolate&&(!founderVisible||!founderRenderers.Contains(r));
                openingStateSet=true;lastOpeningIsolated=isolate;lastFounderVisible=founderVisible;
            }
            if(isolate&&founderVisible&&songTime<0)lord.RenderArrival(songTime);
        }
        public void ResetEffects(){if(built){effects?.Reset();music?.Reset();if(finishing)finishing.Reset();}}
        public Vector3 VoicePosition(int slot)=>slot<0?lord.root.position+Vector3.up*1.9f:roles[Mathf.Clamp(slot,0,9)].root.position+Vector3.up*1.6f;
        public void Render(ShowFrame frame,bool showing)
        {
            if(!built)return;double actual=frame.time;frame=ShowFrame.At(timing.NarrativeTime(actual));frame.musicTime=actual;float t=(float)frame.time;Shader.SetGlobalFloat("_ShowTime",t);
            music.Evaluate((float)actual,showing);story.Render(t,showing);
            Shader.SetGlobalColor("_ParticipantTint",session.Slot>=0?RootKeys.Slots[session.Slot].color:Color.white);
            chamber.gameObject.SetActive(!frame.vortex);vortex.gameObject.SetActive(frame.vortex&&finishing==null);
            well.gameObject.SetActive(chamberArt==null||frame.vortex);
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
            lord.Render(frame);
            if(chamberArt!=null){chamberArt.Battle=interaction.Encounter.State;chamberArt.Evaluate(t,showing,DragonBreath.Target(t,lord.root.position));}
            Vector3 mouth=chamberArt!=null?chamberArt.Mouth:new Vector3(0,9,6),heart=chamberArt!=null?chamberArt.Heart:new Vector3(0,8,11);
            interaction.SetStageTargets(BattleScore.ShieldPoint(lord.root.position),heart,mouth);
            if(!Application.isPlaying||!showing)orcs.Render(frame);interaction.Render((float)actual,t,showing);
            Vector3 audience=Vector3.zero;int people=0;for(int i=0;i<10;i++)if(interaction.Encounter.occupied[i]){audience+=interaction.Encounter.heads[i];people++;}
            audience=people>0?audience/people:new Vector3(0,1.65f,2);
            dragonFireballs.Render(t,showing,mouth,interaction.Encounter.State,config.vfxQuality);
            lord.ApplyBattle(t,heart,interaction.Encounter.State,audience);
            stageMagic.Render((float)actual,showing,mouth,heart);surfaceEcho.Render(t,showing);
            lord.SampleFace((float)actual,showing);concert.harmony=interaction.Encounter.State.harmony;concert.Battle=interaction.Encounter.State;concert.Render((float)actual,showing,lord.root.position,config.vfxQuality);
            if(finishing)finishing.Battle=interaction.Encounter.State;
            groundContact.Render(t>=21&&!frame.vortex);
            if(finishing){finishing.quality=config.vfxQuality;finishing.Render(frame,mouth,heart);}
            else effects.Render(frame,mouth,heart,lord.root.position);
            title.gameObject.SetActive(frame.title&&finishing==null);title.transform.localScale=Vector3.one*Mathf.Lerp(.65f,1,frame.titleProgress);title.color=new Color(.6f+.35f*frame.titleProgress,.88f,1,frame.titleProgress);
            glyph.gameObject.SetActive(t>=34&&t<39&&finishing==null);glyph.localRotation=Quaternion.Euler(90,0,t*8);
            bool counter=t>=34&&t<39,signal=t>=83&&t<87,identify=t>=87&&t<90,rise=t>=92&&t<96;
            call.gameObject.SetActive(showing&&(counter||signal||identify));
            string words=counter?"KEY OF A\nCantors to the front\nFollow the line":signal?"ROOT KEY E\nThe Blade":identify?"E beneath Bb\nThe Discordant":"ALL TEN\nOne frequency";
            if(call.text!=words)call.text=words;
            float start=counter?34:signal?83:identify?87:92,end=counter?39:signal?87:identify?90:96;
            var cueColor=counter?RootKeys.Slots[9].color:signal?RootKeys.Slots[2].color:identify?RootKeys.Slots[8].color:Color.white;
            cueColor.a=Mathf.Min(Mathf.Clamp01((t-start)/.25f),Mathf.Clamp01((end-t)/.35f));call.color=cueColor;
            if(call.gameObject.activeSelf&&Camera.main){
                var view=Camera.main.transform;var forward=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
                if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
                call.transform.position=view.position+forward*2.2f+Vector3.down*.9f;
                call.transform.rotation=Quaternion.LookRotation(forward);
            }
        }
    }
}

