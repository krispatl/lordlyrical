using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LordLyrical
{
    // Presentation only. Combat and network ownership remain in ManualResonanceBeams.
    // One batched ribbon mesh and three bounded particle pools serve all twenty palms.
    public sealed class MagicalPalmVFX : MonoBehaviour
    {
        public const int SparkBudget=1280,CloudBudget=160,FlareBudget=64;
        const int Segments=24,MaxVertices=20*2*Segments*2;
        sealed class Pool
        {
            public ParticleSystem system;
            public ParticleSystemRenderer renderer;
            public ParticleSystem.Particle[] buffer;
            public int count;
        }
        struct Release
        {
            public Vector3 origin,direction;
            public Color color;
            public float at;
            public int seed;
        }
        readonly Release[] releases=new Release[24];
        readonly bool[] previous=new bool[20],seen=new bool[20];
        readonly Vector3[] origins=new Vector3[20],directions=new Vector3[20];
        readonly Color[] colors=new Color[20];
        readonly float[] began=new float[20];
        readonly Vector3[] vertices=new Vector3[MaxVertices];
        readonly Color[] ribbonColors=new Color[MaxVertices];
        readonly Vector2[] uv=new Vector2[MaxVertices];
        readonly int[] indices=new int[20*2*(Segments-1)*6];
        readonly List<Material> owned=new();
        Pool sparks,clouds,flares;
        Mesh ribbons;MeshRenderer ribbonRenderer;
        int vertexCount,indexCount,releaseCursor;
        float time,now,lastTime=-1;
        bool enabledFrame;
        Vector3 eye;
        public int ActiveParticles=>(sparks?.count??0)+(clouds?.count??0)+(flares?.count??0);
        public int DroppedParticles {get;private set;}
        public int RibbonVertices=>vertexCount;
        public int Capacity=>SparkBudget+CloudBudget+FlareBudget;
        public bool Visible=>ribbonRenderer&&ribbonRenderer.enabled||ActiveParticles>0;
        static float Noise(int seed){unchecked{uint n=(uint)seed*747796405u+2891336453u;uint x=((n>>((int)(n>>28)+4))^n)*277803737u;return ((x>>22)^x)/4294967295f;}}
        static void Basis(Vector3 direction,out Vector3 x,out Vector3 y)
        {
            x=Vector3.Cross(direction,Vector3.up).normalized;
            if(x.sqrMagnitude<.01f)x=Vector3.right;
            y=Vector3.Cross(direction,x).normalized;
        }
        Material Own(Material source)
        {
            if(!source)return null;
            var m=new Material(source);m.name=source.name+" | palm FX";
            // Hand effects must remain visible at arm's length. Keep surfaces depth-tested.
            if(m.HasProperty("_Tint"))m.SetColor("_Tint",Color.white);
            if(m.HasProperty("_Monochrome"))m.SetFloat("_Monochrome",1);
            if(m.HasProperty("_NearFade"))m.SetFloat("_NearFade",.06f);
            if(m.HasProperty("_SoftDistance"))m.SetFloat("_SoftDistance",0);
            owned.Add(m);return m;
        }
        public void Initialize(CinematicParticleDirector source,Material filament)
        {
            var fallback=Resources.Load<Material>("Concert/BeatAccents");
            sparks=MakePool("Palm magic | flowing sparks",Own(source?source.sparksMaterial:fallback),SparkBudget,false,true);
            clouds=MakePool("Palm magic | plasma wisps",Own(source?source.energyMaterial:fallback),CloudBudget,true,false);
            flares=MakePool("Palm magic | ignition and contact",Own(source?source.flareMaterial:fallback),FlareBudget,false,false);
            var go=new GameObject("Palm magic | braided current");go.transform.SetParent(transform,false);go.layer=gameObject.layer;
            ribbons=new Mesh{name="All palm currents"};ribbons.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh=ribbons;
            ribbonRenderer=go.AddComponent<MeshRenderer>();ribbonRenderer.sharedMaterial=filament;
            ribbonRenderer.shadowCastingMode=ShadowCastingMode.Off;ribbonRenderer.receiveShadows=false;ribbonRenderer.enabled=false;
            for(int i=0;i<releases.Length;i++)releases[i].at=-100;
        }
        Pool MakePool(string label,Material material,int capacity,bool sheet,bool stretch)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);go.layer=gameObject.layer;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed=false;ps.randomSeed=2501;
            var main=ps.main;main.playOnAwake=false;main.loop=false;main.maxParticles=capacity;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=10;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var tex=ps.textureSheetAnimation;tex.enabled=sheet&&material&&material.HasProperty("_Flipbook")&&material.GetFloat("_Flipbook")>.5f;
            if(tex.enabled){tex.numTilesX=8;tex.numTilesY=8;tex.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));}
            var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;
            r.renderMode=stretch?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
            r.velocityScale=stretch?.018f:0;r.lengthScale=stretch?1.7f:1;r.cameraVelocityScale=0;
            r.maxParticleSize=.07f;r.minParticleSize=0;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.sortMode=ParticleSystemSortMode.None;
            r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});
            ps.Play();ps.Pause();r.enabled=false;
            return new Pool{system=ps,renderer=r,buffer=new ParticleSystem.Particle[capacity]};
        }
        public void Begin(float songTime,float realtime,Vector3 viewer,bool live)
        {
            if(lastTime>=0&&(songTime<lastTime||songTime-lastTime>.75f))Clear();
            lastTime=songTime;time=songTime;now=realtime;eye=viewer;enabledFrame=live;
            vertexCount=indexCount=DroppedParticles=0;
            if(sparks==null)return;
            sparks.count=clouds.count=flares.count=0;Array.Clear(seen,0,seen.Length);
            if(!live)Clear();
        }
        void Add(Pool pool,Vector3 p,float size,Color color,float alpha,float age,int seed,Vector3 velocity=default)
        {
            if(alpha<.003f||size<.002f)return;
            if(pool.count>=pool.buffer.Length){DroppedParticles++;return;}
            color.a=Mathf.Clamp01(alpha);
            pool.buffer[pool.count++]=new ParticleSystem.Particle{
                position=p,velocity=velocity,startSize=size,startColor=color,
                startLifetime=10,remainingLifetime=10*(1-Mathf.Clamp(age,.001f,.98f)),
                rotation3D=new Vector3(0,0,Noise(seed)*360),randomSeed=(uint)(Math.Abs(seed)+1)};
        }
        public void Charge(int id,Vector3 origin,Vector3 direction,Color color,float amount)
        {
            if(!enabledFrame||amount<=0||direction.sqrMagnitude<.1f)return;
            Basis(direction,out var x,out var y);
            for(int k=0;k<16;k++){
                int seed=id*173+k*37;
                float a=k*2.39996f+time*6,rad=Mathf.Lerp(.14f,.025f,amount)*(.5f+Noise(seed)*.5f);
                var p=origin+direction*(.03f+Noise(seed+1)*.055f)+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*rad;
                Add(sparks,p,.014f+amount*.015f,Color.Lerp(color,Color.white,.3f),amount*.70f,.2f,seed);
            }
            Add(flares,origin+direction*.04f,.085f+amount*.06f,color,amount*.40f,.15f,id);
            for(int k=0;k<2;k++)Add(clouds,origin+direction*.065f+x*(k==0?-.02f:.02f),.08f+amount*.035f,color,amount*.12f,Mathf.Repeat(time*.8f+k*.4f,1),id+k);
        }
        public void Beam(int id,Vector3 origin,Vector3 end,Vector3 normal,bool contact,Color color,bool local)
        {
            if(!enabledFrame)return;
            var delta=end-origin;float length=delta.magnitude;if(length<.005f)return;
            var direction=delta/length;seen[id]=true;
            if(!previous[id])began[id]=now;
            previous[id]=true;origins[id]=origin;directions[id]=direction;colors[id]=color;
            Basis(direction,out var x,out var y);
            float age=Mathf.Max(0,now-began[id]),strength=Mathf.Lerp(.65f,1,Mathf.Clamp01(age/.14f));
            int n=local?34:20;
            for(int k=0;k<n;k++){
                int seed=id*3001+k*71+2500;
                float u=Mathf.Repeat(Noise(seed)+time*(5.5f+Noise(seed+1)*3)/Mathf.Max(length,.2f),1);
                float a=Noise(seed+2)*Mathf.PI*2+u*12-time*3;
                float radius=(.018f+.055f*Mathf.Sin(Mathf.PI*u))*(.35f+Noise(seed+3)*.65f);
                var p=origin+delta*u+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*radius;
                float fade=Mathf.Pow(Mathf.Sin(Mathf.PI*u),.4f);
                Add(sparks,p,.008f+Noise(seed+4)*.014f,Color.Lerp(color,Color.white,.35f+Noise(seed+5)*.35f),fade*.85f*strength,u,seed,direction*(2+Noise(seed+6)*3));
            }
            // A restrained plasma envelope gives the beam volume without a wide opaque tube.
            int cloudCount=local?7:3;
            for(int k=0;k<cloudCount;k++){
                int seed=id*631+k*127+8120;float u=Mathf.Repeat(Noise(seed)+time*2.5f/Mathf.Max(length,.2f),1);
                float a=time*2+seed;var offset=(x*Mathf.Sin(a)+y*Mathf.Cos(a))*.025f;
                Add(clouds,origin+delta*u+offset,.08f+Noise(seed+2)*.075f,color,.11f*Mathf.Sin(u*Mathf.PI),Mathf.Repeat(time*.35f+Noise(seed+3),1),seed);
            }
            for(int k=0;k<(local?12:6);k++){
                int seed=id*231+k*53;float u=Mathf.Repeat(Noise(seed)+time*1.8f,1),a=k*2.39996f-time*7;
                float rad=Mathf.Lerp(.07f,.018f,u);
                Add(sparks,origin+direction*(.025f+u*.09f)+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*rad,
                    .009f+.009f*(1-u),Color.Lerp(color,Color.white,.4f),Mathf.Sin(u*Mathf.PI)*.75f,u,seed,direction*.4f);
            }
            Add(flares,origin+direction*.045f,.105f+Mathf.Sin(time*8+id)*.012f,
                Color.Lerp(color,Color.white,.45f),.38f*strength,.2f,id+2900);
            if(age<.22f){
                float u=age/.22f;
                for(int k=0;k<(local?14:6);k++){
                    float a=k*2.39996f;var radial=x*Mathf.Cos(a)+y*Mathf.Sin(a);
                    Add(sparks,origin+direction*(.04f+u*.08f)+radial*(.02f+u*.14f),
                        .014f*(1-u*.5f),color,(1-u)*.8f,u,id*71+k,radial*.4f);
                }
            }
            for(int strand=0;strand<2;strand++)Ribbon(id,strand,origin,end,x,y,color,strength);
            if(contact){
                var nrm=normal.sqrMagnitude>.1f?normal.normalized:-direction;
                Basis(nrm,out var u,out var v);
                for(int k=0;k<(local?10:5);k++){
                    int seed=id*931+k*43;float ageHit=Mathf.Repeat(time*(2.2f+Noise(seed)*1.3f)+Noise(seed+1),1);
                    float a=Noise(seed+2)*6.283185f;var tangent=u*Mathf.Cos(a)+v*Mathf.Sin(a);
                    Add(sparks,end+nrm*(.02f+ageHit*.10f)+tangent*ageHit*.22f,
                        .010f+Noise(seed+3)*.012f,Color.Lerp(color,Color.white,.4f),(1-ageHit)*.8f,ageHit,seed,tangent*.5f+nrm*.3f);
                }
                Add(flares,end+nrm*.028f,.16f+.03f*Mathf.Sin(time*11+id),Color.Lerp(color,Color.white,.55f),.48f,.2f,id+3500);
                Add(clouds,end+nrm*.06f,.22f,color,.18f,Mathf.Repeat(time*.9f+id*.1f,1),id+8100);
            }
        }
        void Ribbon(int id,int strand,Vector3 start,Vector3 end,Vector3 x,Vector3 y,Color color,float strength)
        {
            int first=vertexCount;float length=Vector3.Distance(start,end);
            for(int k=0;k<Segments;k++){
                float u=k/(float)(Segments-1),envelope=Mathf.Sin(u*Mathf.PI);
                float phase=u*(9+length*1.3f)-time*(5.5f+strand*.8f)+strand*Mathf.PI+id*.73f;
                float radius=(.028f+.035f*Mathf.Sin(time*2.3f+u*12+id))*envelope;
                radius=Mathf.Max(.012f*envelope,radius);
                var p=Vector3.Lerp(start,end,u)+(x*Mathf.Cos(phase)+y*Mathf.Sin(phase))*radius;
                Vector3 side=Vector3.Cross((end-start).normalized,(eye-p).normalized).normalized;
                if(side.sqrMagnitude<.01f)side=x;
                float pulse=Mathf.Pow(.5f+.5f*Mathf.Sin(u*24-time*13+id),3);
                float width=(.0035f+.005f*pulse)*envelope;
                Color tint=Color.Lerp(color,Color.white,.16f);tint.a=(.12f+.38f*pulse)*envelope*strength;
                for(int edge=0;edge<2;edge++){
                    vertices[vertexCount]=transform.InverseTransformPoint(p+side*width*(edge==0?-1:1));
                    ribbonColors[vertexCount]=tint;uv[vertexCount]=new Vector2(u,edge);vertexCount++;
                }
                if(k>0){int a=first+(k-1)*2,b=a+2;indices[indexCount++]=a;indices[indexCount++]=b;indices[indexCount++]=a+1;indices[indexCount++]=a+1;indices[indexCount++]=b;indices[indexCount++]=b+1;}
            }
        }
        public void End()
        {
            if(sparks==null)return;
            if(enabledFrame){
                for(int i=0;i<20;i++)if(previous[i]&&!seen[i]){
                    releases[releaseCursor++%releases.Length]=new Release{origin=origins[i],direction=directions[i],color=colors[i],at=now,seed=i*137+releaseCursor};
                    previous[i]=false;
                }
                foreach(var release in releases){
                    float age=now-release.at;if(age<0||age>.28f)continue;
                    float u=age/.28f;Basis(release.direction,out var x,out var y);
                    for(int k=0;k<8;k++){
                        float a=k*2.39996f;var radial=x*Mathf.Cos(a)+y*Mathf.Sin(a);
                        Add(sparks,release.origin+release.direction*.04f+radial*(.035f+u*.07f)+Vector3.up*u*.035f,.012f,release.color,(1-u)*.45f,u,release.seed+k,Vector3.up*.2f);
                    }
                }
            }
            ribbons.Clear(false);
            if(vertexCount>0){
                ribbons.SetVertices(vertices,0,vertexCount);ribbons.SetColors(ribbonColors,0,vertexCount);ribbons.SetUVs(0,uv,0,vertexCount);
                ribbons.SetIndices(indices,0,indexCount,MeshTopology.Triangles,0,true);
            }
            ribbonRenderer.enabled=vertexCount>0;
            Upload(sparks);Upload(clouds);Upload(flares);
        }
        static void Upload(Pool p)
        {
            p.system.SetParticles(p.buffer,p.count);p.system.Simulate(0,false,false,false);p.renderer.enabled=p.count>0;
        }
        public bool AllFinite()
        {
            for(int i=0;i<vertexCount;i++)if(!ManualResonanceBeams.Finite(vertices[i]))return false;
            foreach(var p in new[]{sparks,clouds,flares})if(p!=null)for(int i=0;i<p.count;i++)if(!ManualResonanceBeams.Finite(p.buffer[i].position))return false;
            return true;
        }
        public void Clear()
        {
            Array.Clear(previous,0,previous.Length);Array.Clear(seen,0,seen.Length);
            for(int i=0;i<releases.Length;i++)releases[i].at=-100;
            vertexCount=indexCount=0;
            if(ribbonRenderer)ribbonRenderer.enabled=false;
            if(sparks==null)return;
            sparks.count=clouds.count=flares.count=0;
            sparks.system.Clear();clouds.system.Clear();flares.system.Clear();
            sparks.renderer.enabled=clouds.renderer.enabled=flares.renderer.enabled=false;
        }
        void OnDisable()=>Clear();
        void OnDestroy()
        {
            foreach(var m in owned)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}
            if(ribbons){if(Application.isPlaying)Destroy(ribbons);else DestroyImmediate(ribbons);}
        }
    }
}
