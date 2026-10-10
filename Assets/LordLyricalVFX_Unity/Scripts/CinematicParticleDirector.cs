using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LordLyrical
{
    /// <summary>Native Unity particle systems evaluated from show time. Seeking and late joins never replay simulation.</summary>
    public sealed class CinematicParticleDirector : MonoBehaviour
    {
        public Material sparksMaterial,plumeMaterial,energyMaterial,flareMaterial,debrisMaterial,waveMaterial,shieldMaterial,vortexMaterial;
        public int quality=1;public ConcertState Battle;
        RoleVisual[] roles;OrcEncounterController orcs;LordLyricalController lord;
        Layer sparks,plumes,energy,flares,debris;
        readonly List<Layer> layers=new();
        readonly MeshRenderer[] waves=new MeshRenderer[28];
        readonly Light[] lights=new Light[3];
        MeshRenderer shield,tunnel;Mesh quad,chip,dome,tube;MaterialPropertyBlock block;
        int waveCursor,lightCursor;float t;
        public int ActiveParticles {get;private set;}
        public int ActiveWavefronts=>waveCursor;
        public int DroppedParticles {get;private set;}
        public int ActiveLayers {get;private set;}
        sealed class Layer
        {
            public ParticleSystem system;public ParticleSystemRenderer renderer;public ParticleSystem.Particle[] particles;public int count;
        }
        static float R(int n){unchecked{uint x=(uint)n*747796405u+2891336453u;uint w=((x>>((int)(x>>28)+4))^x)*277803737u;return ((w>>22)^w)/4294967295f;}}
        static bool In(float value,float a,float b)=>value>=a&&value<b;
        static float S(float a,float b,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
        static Vector3 Direction(int seed){float a=R(seed)*Mathf.PI*2,y=R(seed+67)*2-1,r=Mathf.Sqrt(1-y*y);return new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);}
        static Color Key(int i)=>RootKeys.Slots[i].color;
        int Count(int n)=>Mathf.Max(1,Mathf.RoundToInt(n*(quality==0?.45f:quality==1?.75f:1f)));
        public void Initialize(RoleVisual[] actors,OrcEncounterController enemies,LordLyricalController conductor)
        {
            if(layers.Count>0)return;roles=actors;orcs=enemies;lord=conductor;block=new MaterialPropertyBlock();
            quad=AnnularWavefront();
            chip=new Mesh{name="Faceted obsidian particle"};chip.vertices=new[]{new Vector3(0,.8f,0),new Vector3(-.45f,-.4f,.2f),new Vector3(.4f,-.3f,.3f),new Vector3(.05f,-.2f,-.35f)};chip.triangles=new[]{0,2,1,0,1,3,0,3,2,1,2,3};chip.RecalculateNormals();chip.RecalculateBounds();
            sparks=Create("01 | incandescent sparks",sparksMaterial,1800,false,true);
            plumes=Create("02 | rolling volumetric plumes",plumeMaterial,320,true);
            energy=Create("03 | turbulent energy volume",energyMaterial,900,true);
            flares=Create("04 | impact corona",flareMaterial,80);
            debris=Create("05 | physical obsidian fragments",debrisMaterial,300,false,false,true);
            for(int i=0;i<waves.Length;i++){
                var go=Node("Wavefront pool "+i);go.AddComponent<MeshFilter>().sharedMesh=quad;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=waveMaterial;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.enabled=false;waves[i]=r;
            }
            dome=Hemisphere();var sg=Node("Healer | refractive lattice membrane");sg.AddComponent<MeshFilter>().sharedMesh=dome;shield=sg.AddComponent<MeshRenderer>();shield.sharedMaterial=shieldMaterial;shield.shadowCastingMode=ShadowCastingMode.Off;shield.receiveShadows=false;shield.enabled=false;
            tube=Tunnel();var tg=Node("Arrival | turbulent plasma curtains");tg.transform.localPosition=Vector3.up*2;tg.AddComponent<MeshFilter>().sharedMesh=tube;tunnel=tg.AddComponent<MeshRenderer>();tunnel.sharedMaterial=vortexMaterial;tunnel.shadowCastingMode=ShadowCastingMode.Off;tunnel.receiveShadows=false;
            for(int i=0;i<lights.Length;i++){lights[i]=Node("Pooled frequency light "+i).AddComponent<Light>();lights[i].type=LightType.Point;lights[i].shadows=LightShadows.None;lights[i].renderMode=LightRenderMode.ForceVertex;lights[i].enabled=false;}
        }
        GameObject Node(string name){var g=new GameObject(name);g.transform.SetParent(transform,false);g.layer=gameObject.layer;return g;}
        Layer Create(string name,Material mat,int capacity,bool sheet=false,bool stretch=false,bool mesh=false)
        {
            var ps=Node(name).AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=12345;
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.maxParticles=capacity;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=100;main.startSize3D=true;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var tex=ps.textureSheetAnimation;tex.enabled=sheet;
            if(sheet){tex.numTilesX=8;tex.numTilesY=8;tex.animation=ParticleSystemAnimationType.WholeSheet;tex.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));tex.cycleCount=1;}
            var pr=ps.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=mat;pr.renderMode=mesh?ParticleSystemRenderMode.Mesh:stretch?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;pr.alignment=ParticleSystemRenderSpace.View;
            pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;pr.sortMode=mat.HasProperty("_DstBlend")&&mat.GetFloat("_DstBlend")==10?ParticleSystemSortMode.Distance:ParticleSystemSortMode.None;pr.minParticleSize=0;pr.maxParticleSize=.35f;
            if(mesh){pr.mesh=chip;pr.alignment=ParticleSystemRenderSpace.World;}
            else pr.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});
            if(stretch){pr.velocityScale=.04f;pr.lengthScale=2.3f;pr.cameraVelocityScale=0;}
            ps.Play(false);ps.Pause(false);pr.localBounds=new Bounds(new Vector3(0,8,15),Vector3.one*160);var l=new Layer{system=ps,renderer=pr,particles=new ParticleSystem.Particle[capacity]};layers.Add(l);return l;
        }
        void Add(Layer l,Vector3 position,float size,Color color,float alpha,float age,int seed,Vector3 velocity=default)
        {
            if(alpha<=.002f||size<=.003f)return;if(l.count>=l.particles.Length){DroppedParticles++;return;}
            color.a=Mathf.Clamp01(alpha);ref var p=ref l.particles[l.count++];p=new ParticleSystem.Particle{position=position,velocity=velocity,startColor=color,startSize3D=Vector3.one*size,startLifetime=4,remainingLifetime=4*(1-Mathf.Clamp(age,.001f,.985f)),rotation3D=l==debris?new Vector3(seed*17+t*110,seed*29+t*76,seed*11+t*95):new Vector3(0,0,R(seed)*360),randomSeed=(uint)(Math.Abs(seed)+1)};
        }
        void Burst(Vector3 center,float age,Color color,float scale,int seed,bool smoke=true)
        {
            if(age<0||age>3)return;float fade=1-S(.25f,2.2f,age);
            for(int i=0;i<Count(110);i++){
                int s=seed+i*17;Vector3 dir=Direction(s);dir.y=Mathf.Abs(dir.y)*.9f+.12f;float speed=(2+R(s+11)*5)*scale;
                Vector3 p=center+dir*speed*(1-Mathf.Exp(-age*1.8f))/1.8f+Vector3.down*(age*age*1.8f);p.y=.07f+Mathf.Abs(p.y-.07f)*Mathf.Exp(-age*.35f);
                Add(sparks,p,(.018f+R(s+5)*.044f)*scale,Color.Lerp(color,Color.white,.4f),fade*(.4f+R(s+2)*.6f),age/3,s,dir*speed+Vector3.down*age*3);
                if(i%4==0)Add(debris,p,(.06f+R(s+8)*.20f)*scale,new Color(.32f,.38f,.43f),1-S(1.2f,3,age),age/3,s);
            }
            if(smoke)for(int i=0;i<Count(18);i++){
                int s=seed+i*23;var d=Direction(s);d.y=Mathf.Abs(d.y)*.45f;
                var p=center+d*(.2f+age*scale*.85f)+Vector3.up*age*.45f;
                Add(plumes,p,scale*(.55f+age*.9f)*(1+R(s)*.5f),Color.Lerp(new Color(.19f,.23f,.28f),color,.13f),S(0,.16f,age)*(1-S(.7f,3,age))*.48f,Mathf.Clamp01(age/3+R(s)*.1f),s);
            }
            Add(flares,center,scale*(.8f+age*2),Color.Lerp(color,Color.white,.6f),Mathf.Exp(-age*9)*.8f,age/3,seed);
            Wave(center+Vector3.up*.025f,age,2.2f,scale*6,color,Quaternion.Euler(90,0,0),1.1f);
            LightPulse(center+Vector3.up*.8f,color,3*Mathf.Exp(-age*4),5*scale);
        }
        void Stream(Vector3 a,Vector3 b,Color color,float radius,int seed,float strength=1,int count=80)
        {
            var axis=(b-a).normalized;var u=Vector3.Cross(axis,Vector3.up).normalized;if(u.sqrMagnitude<.1f)u=Vector3.right;var v=Vector3.Cross(axis,u);
            int n=Count(count);
            for(int i=0;i<n;i++){
                int s=seed+i*19;float f=Mathf.Repeat(R(s)+t*(.46f+R(s+1)*.2f),1);float angle=R(s+2)*6.283f+t*2+f*15;float rad=radius*(.2f+R(s+4)*.8f)*Mathf.Sin(f*Mathf.PI);
                Vector3 p=Vector3.Lerp(a,b,f)+(u*Mathf.Cos(angle)+v*Mathf.Sin(angle))*rad;
                Add(energy,p,radius*(.6f+R(s+3)*1.3f),color,strength*.28f*Mathf.Sin(f*Mathf.PI),Mathf.Repeat(f*.7f+R(s+7)*.3f,1),s);
                if(i%2==0)Add(sparks,p,.025f+R(s+6)*.025f,Color.Lerp(color,Color.white,.3f),strength*.8f,f,s,axis*12);
            }
            Add(flares,a,radius*2.5f,color,strength*.45f,.4f,seed);Add(flares,b,radius*3,color,strength*.5f,.4f,seed+1);
        }
        void Wave(Vector3 center,float age,float life,float reach,Color color,Quaternion rotation,float strength=1,bool seal=false)
        {
            if(age<=0||age>=life||waveCursor>=waves.Length)return;float f=age/life;var r=waves[waveCursor++];r.enabled=true;r.transform.SetPositionAndRotation(center,rotation);r.transform.localScale=Vector3.one*Mathf.Max(.01f,reach*(1-Mathf.Pow(1-f,2)));
            block.Clear();block.SetColor("_Color",color);block.SetFloat("_Age",f);block.SetFloat("_Energy",strength*2.4f);block.SetFloat("_Style",seal?1:0);r.SetPropertyBlock(block);
        }
        void LightPulse(Vector3 p,Color c,float power,float range)
        {
            if(lightCursor>=lights.Length||power<.03f||quality==0)return;var l=lights[lightCursor++];l.enabled=true;l.transform.position=p;l.color=c;l.intensity=power;l.range=range;l.cullingMask=1<<gameObject.layer;
        }
        void Scan(Vector3 p,float age,bool deep)
        {
            float reach=age*(deep?5.5f:7);Color c=Key(6);if(age<0||reach>35)return;
            Wave(new Vector3(p.x,.08f,p.z),age,deep?5:4,deep?30:26,c,Quaternion.Euler(90,0,0),.8f);
            for(int i=0;i<Count(230);i++){int seed=i*31+419;Vector3 d=Direction(seed);var at=p+d*reach;Add(sparks,at,.025f+R(seed+4)*.045f,c,(1-S(2,4,age))*.55f,.3f,seed);}
        }
        void ConductorAura()
        {
            if(lord==null||t<21||t>=111)return;
            float gain=S(21,22,t)*(1-S(108,111,t));
            float surge=S(92,95,t)*(1-S(99,102,t));
            var center=lord.root.position;
            // Individually phased rising motes fill a body-sized volume. They never form closed rings.
            for(int i=0;i<Count(150);i++){
                int seed=18231+i*79;
                float life=2.8f+R(seed)*2.2f,age=Mathf.Repeat(t+R(seed+1)*life,life),u=age/life;
                float angle=R(seed+2)*6.283185f+age*(.35f+R(seed+3)*.5f);
                float radius=(.36f+R(seed+4)*.28f)*(1+.12f*Mathf.Sin(age*2+seed));
                float y=.10f+u*2.05f;
                Vector3 local=new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius*.8f);
                Vector3 at=center+lord.root.rotation*local;
                float opacity=Mathf.Sin(u*Mathf.PI)*gain;
                Color color=Color.Lerp(new Color(.12f,.52f,1),RootKeys.Spectrum(R(seed+5)),.48f+surge*.2f);
                Add(sparks,at,.016f+R(seed+6)*.026f,color,opacity*(.45f+surge*.25f),u,seed,Vector3.up*.18f);
                if(i%7==0)Add(energy,at,.15f+R(seed+7)*.16f,color,opacity*.13f,u,seed);
            }
        }
        public void Evaluate(float time,Vector3 mouth,Vector3 heart,int detail)
        {
            t=time;quality=detail;waveCursor=lightCursor=0;ActiveParticles=ActiveLayers=DroppedParticles=0;foreach(var l in layers)l.count=0;shield.enabled=false;tunnel.enabled=t<21;block.Clear();block.SetFloat("_Strength",1-S(20,21,t));tunnel.SetPropertyBlock(block);
            ConductorAura();
            if(t<21){
                for(int i=0;i<Count(520);i++){
                    int s=i*13+718;float z=Mathf.Repeat(R(s)*78-t*(8+R(s+7)*3),78)-6;float angle=R(s+1)*6.283f+z*.08f+t*.08f,r=2.7f+R(s+2)*2.5f;Vector3 p=new Vector3(Mathf.Cos(angle)*r,2+Mathf.Sin(angle)*r,z);
                    Add(sparks,p,.028f+R(s+5)*.06f,RootKeys.Spectrum(R(s+4)),.48f,.3f,s,Vector3.back*10);
                    if(i%7==0)Add(energy,p,1.3f+R(s+8)*1.4f,Color.Lerp(new Color(.1f,.08f,.5f),Color.cyan,R(s+2)),.10f,Mathf.Repeat(t*.07f+R(s),1),s);
                }
                LightPulse(new Vector3(0,2,6),new Color(.12f,.34f,1),1.1f,12);
            }
            if(In(t,20,24))Burst(new Vector3(0,.2f,3),t-21,new Color(.3f,.72f,1),1.5f,151);
            if(t>=21)for(int i=0;i<Count(100);i++){
                int s=i*83+914;Vector3 p=new Vector3(R(s)*25-12.5f,.3f+R(s+2)*8,2+R(s+3)*23);p+=new Vector3(Mathf.Sin(t*.14f+s),Mathf.Sin(t*.12f+s)*.4f,Mathf.Cos(t*.11f+s))*.22f;
                Add(sparks,p,.014f+R(s+4)*.025f,new Color(.34f,.47f,.64f),.10f+Mathf.Pow(.5f+.5f*Mathf.Sin(t*.7f+s),8)*.3f,.5f,s);
            }
            if(In(t,26,30))for(int k=0;k<3;k++)Burst(new Vector3((k-1)*3,.15f,16),t-26-k*.13f,Key(0),.85f,400+k*331);
            if(In(t,31,35)&&roles[6].HasParticipant)Scan(roles[6].Hand(false),t-31,false);
            if(In(t,47,50)||In(t,70,81.5f)){
                float start=t<60?47:70,end=t<60?50:81.5f;float strength=S(start,start+.45f,t)*(1-S(end-.8f,end,t));shield.enabled=true;shield.transform.position=new Vector3(0,.04f,0);shield.transform.localScale=Vector3.one*6.2f;
                Vector3 hit=new Vector3(Mathf.Sin((t-74)*.75f)*5,2,-2);float impact=.2f;
                if(t>=70){
                    bool contact=DragonBreath.TryWardContact(mouth,DragonBreath.Target(t,lord.root.position),shield.transform.position,shield.transform.lossyScale.x,out hit);
                    impact=contact?DragonBreath.Strength(t)*Mathf.Lerp(.2f,1,DragonBreath.SweepBlend(t)):0;
                }
                block.Clear();block.SetColor("_Color",Key(4));block.SetFloat("_Strength",strength);block.SetVector("_Impact",new Vector4(hit.x,hit.y,hit.z,impact));shield.SetPropertyBlock(block);
                Wave(new Vector3(0,.09f,0),Mathf.Repeat(t-start,2),2,7.8f,Key(4),Quaternion.Euler(90,0,0),.32f,true);
            }
            if(In(t,52,55))for(int i=0;i<Count(220);i++){
                int s=i*33+523;float age=Mathf.Clamp01((t-52-R(s)*1.2f)/1.8f);float x=R(s+1)*16-8,z=3+R(s+3)*17;Vector3 p=new Vector3(x,(1-age)*(1-age)*2+.07f,z);
                Add(sparks,p,.04f,Key(7),Mathf.Sin(age*Mathf.PI)*.7f,age,s,Vector3.down*2);
            }
            if(In(t,55,59)&&roles[6].HasParticipant)Scan(roles[6].Hand(false),(t-55)*.7f,false);
            if(In(t,59,61.5f))for(int i=0;i<12;i++)Wave(lord.root.position+Vector3.up*2,t-59,2.5f,26,RootKeys.Spectrum(i/12f),Quaternion.Euler(i*15,i*30,0),.17f);
            if(In(t,63,68))for(int k=0;k<3;k++){
                float a=t-63-k*.11f;Burst(new Vector3((k-1)*7,.14f,12),a,new Color(.32f,.48f,.7f),1.4f,6300+k*217);
                for(int i=0;i<Count(24);i++){int s=k*100+i*7;var p=new Vector3((k-1)*7+R(s)*2-1,7+R(s+1)*4-a*a*1.4f,12+R(s+2)*2-1);p.y=Mathf.Max(.12f,p.y);Add(debris,p,.08f+R(s+8)*.28f,new Color(.3f,.35f,.42f),1-S(66,68,t),.4f,s);}
            }
            if(In(t,66.3f,69))for(int k=0;k<3;k++)Wave(mouth,t-66.3f-k*.13f,2.4f,7,new Color(.35f,.65f,1),Quaternion.identity,.7f);
            if(In(t,68.2f,69.2f)){float charge=S(68.2f,69,t);Add(flares,mouth,1+charge*1.7f,new Color(.4f,.75f,1),DragonBreath.ChargeStrength(t),.5f,6900);}
            if(In(t,69,81)){
                var target=DragonBreath.Target(t,lord.root.position);
                float strength=DragonBreath.Strength(t);Stream(mouth,target,new Color(.13f,.42f,1),Mathf.Lerp(.38f,.62f,DragonBreath.SweepBlend(t)),6900,strength,200);
                for(int i=0;i<Count(48);i++){int s=i*71+701;var p=target+Direction(s)*(.2f+R(s+2)*.7f);Add(sparks,p,.035f+R(s+4)*.05f,Color.Lerp(Key(4),Color.white,.5f),strength*.75f,.5f,s,Direction(s)*5);}
                LightPulse(target,Key(4),strength*2.5f,7);LightPulse(mouth,new Color(.15f,.4f,1),strength*3,8);

            }
            float counter=BattleScore.Counter(t);
            if(counter>0){
                Stream(lord.rightHand.position,heart,new Color(.22f,.68f,1),.14f,7600,counter,48);
                Add(flares,heart,.65f,new Color(.5f,.85f,1),counter*.45f,.4f,7610);
                for(int i=0;i<Count(35);i++){int s=i*73+7620;float age=Mathf.Repeat(t*1.8f+R(s),1);Add(sparks,heart+Direction(s)*(age*1.5f),.03f,new Color(.4f,.8f,1),(1-age)*counter,.5f,s,Direction(s)*3);}
            }
            float rescue=BattleScore.Rescue(t);
            if(rescue>0){
                float charge=Battle?.rescueCharge??0;Vector3 center=BattleScore.ShieldPoint(lord.root.position);
                for(int i=0;i<Count(64);i++){
                    int s=7900+i*31;float angle=R(s)*6.283f,y=R(s+4)*2-1;float r=Mathf.Sqrt(1-y*y);
                    Vector3 d=new Vector3(Mathf.Cos(angle)*r,y,Mathf.Abs(Mathf.Sin(angle)*r));
                    Add(sparks,center+d*(.78f+Mathf.Sin(t*9+i)*(.035f*(1-charge))),.025f+charge*.015f,Color.Lerp(new Color(1,.23f,.06f),new Color(.2f,.8f,1),charge),rescue*(.45f+charge*.4f),.5f,s);
                }
                LightPulse(center,Color.Lerp(new Color(1,.25f,.03f),Color.cyan,charge),rescue*(.3f+charge),3);
            }
            if(In(t,83,90)&&roles[6].HasParticipant)Scan(roles[6].Hand(false),Mathf.Repeat(t-83,3.5f),true);
            if(In(t,90,93)){
                
                Burst(heart,t-90,Key(8),1.1f,9090);
            }
            if(In(t,92,101))for(int k=0;k<10;k++){
                if(!roles[k].HasParticipant)continue;float charge=S(92,98,t),angle=k*Mathf.PI/5;var orbit=new Vector3(Mathf.Sin(angle)*2,3.7f+Mathf.Cos(angle)*2,6);var hand=roles[k].Hand(false);
                if(Battle==null||(Battle.supportMask&(1<<k))==0)continue;charge*=Battle.finaleCharge;
                for(int i=0;i<Count(15);i++){int s=k*317+i*31;float f=Mathf.Repeat(t*.4f+R(s),1);Vector3 p=hand+new Vector3(Mathf.Sin(f*7+s)*.18f,f*2,Mathf.Cos(f*7+s)*.18f);Add(sparks,p,.035f,Key(k),charge*Mathf.Sin(f*Mathf.PI),f,s,Vector3.up*3);}
            }
            if(In(t,98,104)){
                float a=t-98;Wave(heart,a,3.3f,12,Color.white,Quaternion.identity,.75f);
                // Authored finishing hit is locked to the song, including with no hand input.
                Burst(heart,a,new Color(.35f,.75f,1),1.7f,9824);
                for(int i=0;i<Count(380);i++){int s=i*61+9801;float delay=R(s)*1.7f,u=Mathf.Clamp01((a-delay)/3);if(u<=0||u>=1)continue;Vector3 d=Direction(s);Vector3 p=Vector3.Lerp(heart+d*3,new Vector3(0,.2f,10),u)+new Vector3(Mathf.Sin(u*13+s),0,Mathf.Cos(u*13+s))*Mathf.Sin(u*Mathf.PI)*2.3f;Add(sparks,p,(.025f+R(s+5)*.065f)*(1-u*.6f),AssignedColour40(s),Mathf.Sin(u*Mathf.PI),u,s);if(i%7==0)Add(energy,p,.45f,AssignedColour40(s),Mathf.Sin(u*Mathf.PI)*.16f,u,s);}
                LightPulse(heart,Color.cyan,1.5f*(1-S(98,101,t)),9);
            }
            if(In(t,101,106)){Wave(new Vector3(0,.08f,10),t-101,5,24,Color.cyan,Quaternion.Euler(90,0,0),.7f);Wave(new Vector3(0,.1f,10),t-101-.25f,4.5f,22,new Color(.52f,.34f,1),Quaternion.Euler(90,0,0),.5f,true);}
            if(In(t,103,119))for(int i=0;i<Count(160);i++){int s=i*107+1101;float y=Mathf.Repeat(R(s)*7+t*.21f,7);Add(sparks,new Vector3(R(s+1)*19-9.5f,y,2+R(s+3)*19),.015f+R(s+4)*.025f,AssignedColour40(s),S(103,105,t)*(1-S(115,119,t))*.3f,.4f,s);}
            for(int i=waveCursor;i<waves.Length;i++)waves[i].enabled=false;for(int i=lightCursor;i<lights.Length;i++)lights[i].enabled=false;
            foreach(var l in layers){l.system.SetParticles(l.particles,l.count);l.system.Simulate(0,false,false,false);l.renderer.enabled=l.count>0;ActiveParticles+=l.count;if(l.count>0)ActiveLayers++;}
        }
        public void EvaluateArrival(float elapsed,Vector3 point)
        {
            t=elapsed;waveCursor=lightCursor=0;ActiveParticles=ActiveLayers=DroppedParticles=0;
            foreach(var l in layers)l.count=0;shield.enabled=false;tunnel.enabled=false;
            float age=elapsed-15;
            if(age>=0&&age<2.3f){
                float gain=S(0,.07f,age)*(1-S(1.5f,2.3f,age));
                Stream(point+Vector3.up*18,point+Vector3.up*.4f,new Color(.15f,.55f,1),.32f,15531,gain,140);
            }
            if(age>=1.2f&&age<4.2f)Burst(point+Vector3.up*.12f,age-1.2f,new Color(.25f,.65f,1),.95f,16041);
            float music=elapsed-17;
            if(music>=0&&music<9){
                float engulf=OpeningTiming.Smooth(4,8,music);
                float radius=OpeningTiming.PortalRadius(music);
                Vector3 center=OpeningTiming.PortalCenter;
                for(int i=0;i<Count(160);i++){
                    int seed=i*41+717;float angle=R(seed)*6.283f+music*(.35f+R(seed+1)*.2f);
                    var tangent=new Vector3(-Mathf.Sin(angle),Mathf.Cos(angle),0);
                    var pos=center+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*radius*(.97f+R(seed+3)*.08f);
                    Add(sparks,pos,.02f+R(seed+8)*.04f,Color.Lerp(Color.cyan,new Color(.5f,.18f,1),R(seed+9)),(1-S(8,9,music))*.8f,.4f,seed,tangent*3);
                }
            }
            for(int i=waveCursor;i<waves.Length;i++)waves[i].enabled=false;for(int i=lightCursor;i<lights.Length;i++)lights[i].enabled=false;
            foreach(var l in layers){l.system.SetParticles(l.particles,l.count);l.system.Simulate(0,false,false,false);l.renderer.enabled=l.count>0;ActiveParticles+=l.count;if(l.count>0)ActiveLayers++;}
        }
        public void Clear(){foreach(var l in layers){l.count=0;l.system.Clear(true);l.renderer.enabled=false;}foreach(var r in waves)if(r)r.enabled=false;foreach(var l in lights)if(l)l.enabled=false;if(shield)shield.enabled=false;if(tunnel)tunnel.enabled=false;ActiveParticles=ActiveLayers=waveCursor=0;}
        Color AssignedColour40(int seed)
        {
            int slot=Mathf.Abs(seed%10);
            return roles[slot].HasParticipant?RootKeys.Slots[slot].color:Color.black;
        }
        public ulong Fingerprint(){unchecked{ulong hash=1469598103934665603UL;foreach(var l in layers){hash=(hash^(uint)l.count)*1099511628211UL;for(int i=0;i<l.count;i++){var p=l.particles[i];hash=(hash^(uint)Mathf.RoundToInt(p.position.x*10000))*1099511628211UL;hash=(hash^(uint)Mathf.RoundToInt(p.position.y*10000))*1099511628211UL;hash=(hash^(uint)Mathf.RoundToInt(p.position.z*10000))*1099511628211UL;hash=(hash^p.randomSeed)*1099511628211UL;}}return hash;}}
        public bool AllFinite(){foreach(var l in layers)for(int i=0;i<l.count;i++){var p=l.particles[i].position;if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))return false;}return true;}
        static Mesh Hemisphere(){var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int rings=16,slices=48;for(int j=0;j<=rings;j++)for(int i=0;i<=slices;i++){float a=i/(float)slices*Mathf.PI*2,p=j/(float)rings*Mathf.PI*.5f;v.Add(new Vector3(Mathf.Cos(a)*Mathf.Cos(p),Mathf.Sin(p),Mathf.Sin(a)*Mathf.Cos(p)));uv.Add(new Vector2(i/(float)slices,j/(float)rings));}for(int j=0;j<rings;j++)for(int i=0;i<slices;i++){int a=j*(slices+1)+i,b=a+slices+1;tr.AddRange(new[]{a,b,a+1,a+1,b,b+1});}var m=new Mesh{name="Healer hemisphere"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        static Mesh AnnularWavefront(){var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int n=96;for(int i=0;i<=n;i++)for(int j=0;j<2;j++){float a=i/(float)n*Mathf.PI*2,r=j==0?.39f:1;var p=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);v.Add(p);uv.Add(new Vector2(p.x*.5f+.5f,p.y*.5f+.5f));}for(int i=0;i<n;i++){int a=i*2;tr.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}var m=new Mesh{name="Annular wavefront | empty centre not rasterized"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateBounds();return m;}
        static Mesh Tunnel(){var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int rings=48,slices=64;for(int j=0;j<=rings;j++)for(int i=0;i<=slices;i++){float a=i/(float)slices*Mathf.PI*2,z=-8+j/(float)rings*88;v.Add(new Vector3(Mathf.Cos(a)*5.5f,Mathf.Sin(a)*5.5f,z));uv.Add(new Vector2(i/(float)slices,j/(float)rings));}for(int j=0;j<rings;j++)for(int i=0;i<slices;i++){int a=j*(slices+1)+i,b=a+slices+1;tr.AddRange(new[]{a,b,a+1,a+1,b,b+1});}var m=new Mesh{name="Flowing plasma tube"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateBounds();return m;}
        void OnDestroy(){foreach(var m in new[]{quad,chip,dome,tube})if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}
