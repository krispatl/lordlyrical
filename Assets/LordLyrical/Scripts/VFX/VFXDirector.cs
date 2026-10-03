using UnityEngine;
namespace LordLyrical
{
    // Each effect is a bounded pool member addressed by production-plan ID 1..30.
    // The live envelope is reconstructed from show time, including a late join mid-effect.
    public sealed class VFXDirector
    {
        readonly Transform[] rings=new Transform[31];readonly Material[] materials=new Material[31];
        readonly LineRenderer[] beams=new LineRenderer[12];readonly Cue[] cues;
        readonly LineRenderer[] trails=new LineRenderer[10];readonly Transform[] footsteps=new Transform[10];readonly Material[] footMaterials=new Material[10];
        readonly float[] strengths=new float[31],ages=new float[31];readonly Transform[] word=new Transform[10];
        readonly MaterialPropertyBlock block=new();
        public VFXDirector(Transform parent,Cue[] table)
        {
            cues=table;var mesh=ResonantGeometry.Ring(1,.025f,72);
            for(int id=1;id<=30;id++){materials[id]=new Material(Shader.Find("LordLyrical/Wave"));materials[id].SetColor("_Color",RootKeys.Spectrum(id/30f));rings[id]=ResonantGeometry.MeshObject("VFX-"+id.ToString("00"),parent,mesh,materials[id],Vector3.up*.03f,Vector3.one).transform;}
            for(int i=0;i<12;i++){var m=new Material(Shader.Find("LordLyrical/Wave"));m.SetColor("_Color",i<10?RootKeys.Slots[i].color:Color.red);beams[i]=ResonantGeometry.Line("Harmonic beam "+i,parent,m,24,.05f);}
            for(int i=0;i<10;i++)word[i]=ResonantGeometry.MeshObject("Word frequency "+i,parent,mesh,materials[28],new Vector3(0,4,8),Vector3.one).transform;
            for(int i=0;i<10;i++){var m=new Material(Shader.Find("LordLyrical/Wave"));m.SetColor("_Color",RootKeys.Slots[i].color);footMaterials[i]=m;footsteps[i]=ResonantGeometry.MeshObject("Footstep cymatics "+i,parent,mesh,m,Vector3.zero,Vector3.one).transform;trails[i]=ResonantGeometry.Line("Floor trail "+i,parent,m,8,.045f);}
        }
        public void Reset(){for(int i=1;i<=30;i++)rings[i].gameObject.SetActive(false);foreach(var b in beams)b.enabled=false;foreach(var w in word)w.gameObject.SetActive(false);foreach(var f in footsteps)f.gameObject.SetActive(false);foreach(var trail in trails)trail.enabled=false;}
        public void Render(ShowFrame frame,Vector3 mouth,Vector3 heart,Vector3 lord)
        {
            float t=(float)frame.time;System.Array.Clear(strengths,0,31);
            foreach(var c in cues){double age=frame.musicTime-c.time;if(age<0||age>=c.duration)continue;foreach(int id in c.vfx){float envelope=Mathf.Min(1,(float)age*5)*Mathf.Min(1,(float)(c.duration-age)*2);if(envelope>=strengths[id]){strengths[id]=envelope;ages[id]=(float)age;}}}
            for(int id=1;id<=30;id++)
            {
                bool active=strengths[id]>0;var r=rings[id];r.gameObject.SetActive(active);if(!active)continue;
                float a=ages[id];float radius=id==14?7:1+a*(id==18?8:4);
                r.localScale=Vector3.one*radius;r.localPosition=new Vector3(0,id==18?2.2f:.04f+id*.002f,id==7||id==18?5:3);
                r.localRotation=Quaternion.Euler(id==18||id>=19?80:0,t*(id==15?90:0),0);
                Color col=id==7||id==17?RootKeys.Slots[6].color:id==14?RootKeys.Slots[4].color:id==25?RootKeys.Slots[8].color:RootKeys.Spectrum(id/30f);
                materials[id].SetColor("_Color",col);materials[id].SetFloat("_Gain",strengths[id]*(id==28?4:1.7f));
            }
            bool firing=t>=98&&t<101,barrage=t>=81&&t<83,anti=t>=90&&t<92,shatter=t>=39&&t<41,titleAssembly=t>=111&&t<117;
            for(int i=0;i<10;i++)
            {
                bool steps=(t>=21&&t<54||t>=92&&t<101)&&!(i==5&&t>=25&&t<50);footsteps[i].gameObject.SetActive(steps);trails[i].enabled=steps&&t<54;
                float step=Mathf.Repeat(t-21,1.2f);var position=RoleVisual.CinematicPosition(i,t);position.y=.035f;footsteps[i].position=position;footsteps[i].localScale=Vector3.one*(.2f+step*.8f);footMaterials[i].SetFloat("_Gain",(1-step/1.2f)*1.6f);
                if(trails[i].enabled)for(int j=0;j<8;j++){var p=RoleVisual.CinematicPosition(i,t-j*.14f);p.y=.028f;trails[i].SetPosition(j,p);}
                bool active=firing||titleAssembly||barrage&&(i==2||i==3)||anti&&i==8||shatter&&i==2;
                beams[i].enabled=active;if(!active)continue;
                Vector3 start=RootKeys.Slots[i].spawn+Vector3.up*1.5f,target=shatter?new Vector3(-4,1.8f,12):heart;
                if(titleAssembly){float angle=i*Mathf.PI*2/10;start=new Vector3(Mathf.Sin(angle)*16,5,3+Mathf.Cos(angle)*16);target=new Vector3((i-4.5f)*.8f,6-(t-111)*.65f,9);}
                for(int j=0;j<24;j++){float u=j/23f;var p=Vector3.Lerp(start,target,u);p+=Vector3.up*Mathf.Sin(u*30-t*12+i)*.1f*Mathf.Sin(u*Mathf.PI);beams[i].SetPosition(j,p);}
                beams[i].widthMultiplier=firing?.09f:.06f;
            }
            bool breath=t>=69&&t<81;beams[10].enabled=breath;beams[11].enabled=breath&&t>=74;
            if(breath){Vector3 end=t<74?lord+Vector3.up*1.5f:new Vector3(Mathf.Sin((t-74)*.6f)*10,1,-3);for(int b=10;b<12;b++){beams[b].widthMultiplier=t<74?.2f:1.1f;for(int j=0;j<24;j++){float u=j/23f;beams[b].SetPosition(j,Vector3.Lerp(mouth,end,u)+Vector3.up*Mathf.Sin(u*40-t*9)*.08f);}}}
            for(int i=0;i<10;i++){word[i].gameObject.SetActive(firing);if(!firing)continue;float age=t-98;word[i].localPosition=Vector3.Lerp(new Vector3(0,2,3),heart,Mathf.Clamp01(age+i*.025f));word[i].localScale=Vector3.one*(.5f+i*.18f+age);word[i].localRotation=Quaternion.Euler(90,i*18,0);}
        }
    }
}
