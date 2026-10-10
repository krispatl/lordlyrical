using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    // Successful authority-confirmed guard only; no head-locked flash or camera motion.
    public sealed class GuardContactPresentation : MonoBehaviour
    {
        public const int Budget=96;
        readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[Budget];
        ParticleSystem particles;Vector3 origin,normal;float at=-1000;int count;
        public int ActiveParticles=>count;
        public void Initialize()
        {
            particles=gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=particles.main;m.playOnAwake=false;m.loop=false;m.maxParticles=Budget;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startSpeed=0;
            var e=particles.emission;e.enabled=false;var s=particles.shape;s.enabled=false;
            var r=GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Resources.Load<Material>("Concert/BeatAccents");r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.025f;
            particles.Play();particles.Pause();
        }
        public void Trigger(Vector3 point,Vector3 direction,float time)
        {
            if(!ManualResonanceBeams.Finite(point)||!ManualResonanceBeams.Finite(direction)||direction.sqrMagnitude<.01f||!float.IsFinite(time))return;
            origin=point;normal=direction.normalized;at=time;
        }
        public void Render(float t,bool visible)
        {
            count=0;float age=t-at,alpha=visible?Combat34Score.GuardEnvelope(age):0;
            if(alpha>0){
                Vector3 x=Vector3.Cross(normal,Vector3.up).normalized;if(x.sqrMagnitude<.01f)x=Vector3.right;Vector3 y=Vector3.Cross(normal,x);
                for(int i=0;i<Budget;i++){
                    float angle=i*2.399963f,speed=.4f+CinemaPresentation.Hash(i*31)*.65f;
                    Vector3 radial=x*Mathf.Cos(angle)+y*Mathf.Sin(angle);
                    Vector3 p=origin+radial*(.07f+age*speed)+normal*age*.16f;
                    float near=Camera.main?CinemaTiming.S(.12f,.38f,Vector3.Distance(p,Camera.main.transform.position)):1;
                    Color c=Color.Lerp(new Color(.15f,.75f,1),new Color(.7f,.94f,1),CinemaPresentation.Hash(i+33));c.a=alpha*.55f*near;
                    buffer[count++]=new ParticleSystem.Particle{position=p,startSize=.007f+CinemaPresentation.Hash(i+19)*.012f,startColor=c,startLifetime=1,remainingLifetime=1,randomSeed=(uint)(i+1)};
                }
            }
            particles.SetParticles(buffer,count);particles.Simulate(0,false,false,false);
        }
        public void Clear(){at=-1000;count=0;if(particles)particles.Clear();}
        void OnDisable()=>Clear();
    }
}
