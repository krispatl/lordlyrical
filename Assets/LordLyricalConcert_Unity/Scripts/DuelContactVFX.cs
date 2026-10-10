using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    // Song-time particles: seeking, replay and remote snapshots use the same envelopes.
    public sealed class DuelContactVFX : MonoBehaviour
    {
        public const int Budget=512;
        readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[Budget];
        ParticleSystem particles;int count;
        public int ActiveParticles=>count;
        static readonly Color Ice=new Color(.18f,.7f,1),Gold=new Color(1,.72f,.28f);
        public void Initialize()
        {
            particles=gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=particles.main;m.playOnAwake=false;m.loop=false;m.maxParticles=Budget;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startSpeed=0;
            var e=particles.emission;e.enabled=false;var s=particles.shape;s.enabled=false;
            var r=GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Resources.Load<Material>("Concert/BeatAccents");r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.07f;
            r.localBounds=new Bounds(new Vector3(0,8,12),new Vector3(40,30,50));particles.Play();particles.Pause();
        }
        static float Noise(int n)=>Mathf.Repeat(Mathf.Sin(n*127.1f+13.2f)*43758.5453f,1);
        void Add(Vector3 p,float size,Color colour,float alpha)
        {
            if(count>=Budget||alpha<=.001f)return;
            colour.a=Mathf.Clamp01(alpha);
            buffer[count++]=new ParticleSystem.Particle{position=p,startSize=size,startColor=colour,startLifetime=1,remainingLifetime=1,randomSeed=(uint)(count+1)};
        }
        void Burst(Vector3 center,Vector3 normal,float age,Color colour,int detail,float scale)
        {
            if(age<0||age>=.8f)return;
            var x=Vector3.Cross(normal,Vector3.up).normalized;if(x.sqrMagnitude<.01f)x=Vector3.right;
            var y=Vector3.Cross(normal,x).normalized;float fade=1-age/.8f;
            for(int i=0;i<detail;i++){
                float angle=i*2.399963f;var radial=x*Mathf.Cos(angle)+y*Mathf.Sin(angle);
                var velocity=radial*(.7f+Noise(i)*2.5f)+normal*(.3f+Noise(i+31)*1.2f);
                Add(center+velocity*age*scale+Vector3.down*age*age*.5f,(.025f+Noise(i+47)*.035f)*fade,Color.Lerp(colour,Gold,Noise(i+17)*.3f),fade*.8f);
            }
            // Broken expanding halo instead of an opaque impact sphere.
            for(int i=0;i<detail/2;i++){
                float a=i*6.283185f/(detail/2);
                Add(center+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*(.12f+age*1.8f)*scale,.045f*fade,colour,fade*fade*.7f);
            }
        }
        public void Render(float t,Vector3 palm,Vector3 heart,ConcertState state,int quality)
        {
            count=0;if(t>=67&&t<101){
                int detail=quality==0?32:64;Vector3 direction=(heart-palm).normalized;
                var x=Vector3.Cross(direction,Vector3.up).normalized;if(x.sqrMagnitude<.01f)x=Vector3.right;
                var y=Vector3.Cross(direction,x).normalized;
                float charge=Mathf.Max(HeroDuelScore.Anticipation(t),HeroDuelScore.FinalGather(t)*.8f);
                if(charge>0)for(int i=0;i<detail;i++){
                    float u=Mathf.Repeat(i/(float)detail+t*1.2f,1),a=i*2.399963f+t*3;
                    Add(palm+direction*.08f+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*(1-u)*.24f,.016f+u*.019f,Ice,charge*u*.8f);
                }
                float counter=BattleScore.Counter(t);
                if(counter>0)for(int i=0;i<detail;i++){
                    float u=Mathf.Repeat(i/(float)detail+t*2.1f,1),a=i*2.399963f+t*4;
                    Add(Vector3.Lerp(palm,heart,u)+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*.07f*Mathf.Sin(u*Mathf.PI),.025f,Ice,counter*.7f);
                }
                Burst(heart,-direction,HeroDuelScore.ImpactAge(t),Ice,detail,1);
                if(state!=null&&state.dragonHitSerial>0)Burst(state.dragonHitPoint,-direction,t-state.dragonHitAt,Gold,detail,.7f);
            }
            particles.SetParticles(buffer,count);particles.Simulate(0,false,false,false);
        }
        public void Clear(){count=0;if(particles)particles.Clear();}
        void OnDisable()=>Clear();
    }
}
