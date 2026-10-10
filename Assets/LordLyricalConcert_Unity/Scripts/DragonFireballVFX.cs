using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    // Fixed pool, no particle collisions or per-shot allocations. Time comes from the song.
    public sealed class DragonFireballVFX : MonoBehaviour
    {
        public const int Budget=720;
        ParticleSystem particles;readonly ParticleSystem.Particle[] pool=new ParticleSystem.Particle[Budget];int count;
        public int ActiveParticles=>count;
        public int WarningParticles {get;private set;}
        public int GuardParticles {get;private set;}
        static readonly Color Gold=new Color(1.9f,.38f,.045f),Core=new Color(2.4f,1.7f,.65f);
        public void Initialize()
        {
            particles=gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=particles.main;m.playOnAwake=false;m.loop=false;m.maxParticles=Budget;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startSpeed=0;
            var e=particles.emission;e.enabled=false;var s=particles.shape;s.enabled=false;
            var r=GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Resources.Load<Material>("Concert/BeatAccents");r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.12f;
            r.localBounds=new Bounds(new Vector3(0,6,10),new Vector3(40,30,50));particles.Play();particles.Pause();
        }
        static float Noise(int n)=>Mathf.Repeat(Mathf.Sin(n*127.1f+31.7f)*43758.5453f,1);
        static Vector3 Direction(int n){float y=Noise(n)*2-1,a=Noise(n+91)*Mathf.PI*2,r=Mathf.Sqrt(1-y*y);return new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);}
        void Add(Vector3 p,float size,Color color)
        {
            if(count>=Budget||size<=0)return;
            // Avoid a full-screen flare if a player walks into a projectile.
            if(Camera.main)color*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.65f,Vector3.Distance(Camera.main.transform.position,p)));
            pool[count++]=new ParticleSystem.Particle{position=p,startSize=size,startColor=color,startLifetime=1,remainingLifetime=1,randomSeed=(uint)(count+1)};
        }
        public void Render(float t,bool showing,Vector3 mouth,ConcertState state,int quality)
        {
            count=0;WarningParticles=GuardParticles=0;
            if(showing&&state?.dragonShots!=null)for(int i=0;i<state.dragonShots.Length;i++){
                var shot=state.dragonShots[i];if(!shot.armed)continue;
                float charge=DragonFireballScore.Charge(t,i),age=t-DragonFireballScore.Launch(i);
                int detail=quality==0?32:64;
                float warning=Combat34Score.FireballWarning(t,i,shot);
                if(warning>0){
                    Vector3 floor=new Vector3(shot.target.x,.035f,shot.target.z);
                    for(int j=0;j<32;j++){
                        float angle=j*Mathf.PI/16;float radius=.77f;
                        Add(floor+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius,.024f,Gold*warning*.35f);WarningParticles++;
                        if(j%4==0){Add(floor+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(radius+.07f),.04f,Core*warning*.24f);WarningParticles++;}
                    }
                }
                if(charge>0){
                    Add(mouth,.14f+charge*.45f,Core*charge);
                    for(int j=0;j<detail;j++){float p=Mathf.Repeat(j/(float)detail+t*1.6f,1);var d=Direction(j+i*133);Add(mouth+d*(1-p)*(.55f+.25f*charge),.024f+.04f*p,Gold*charge*p);}
                }
                if(shot.launched&&!shot.resolved&&age>=0&&age<DragonFireballScore.Flight){
                    var center=DragonFireballScore.Position(shot,age);Add(center,.42f,Core);Add(center,.78f,Gold*.5f);
                    for(int j=0;j<detail*3;j++){
                        float lag=Noise(j+11)*.36f;if(age<lag)continue;
                        var pos=DragonFireballScore.Position(shot,age-lag)+Direction(j+i*219)*(.077f+lag*.73f);
                        float fade=1-lag/.36f;Add(pos,(.063f+.196f*Noise(j+37))*fade,Color.Lerp(Gold,Core,Noise(j+9)*.45f)*fade);
                    }
                }
                float impact=t-(shot.impactTime>0?shot.impactTime:DragonFireballScore.Launch(i)+DragonFireballScore.Flight);
                Vector3 impactPoint=shot.impactTime>0?shot.impactPoint:shot.target;
                if(shot.launched&&impact>=0&&impact<DragonFireballScore.Afterglow){
                    float fade=1-impact/DragonFireballScore.Afterglow;Color colour=shot.blocked?(shot.blockerSlot>=0?RootKeys.Slots[shot.blockerSlot].color*1.6f:new Color(.15f,1.6f,2)):Gold;
                    if(shot.blocked){
                        Vector3 normal=shot.impactTime>0?shot.impactNormal:(shot.origin-shot.target).normalized,x=Vector3.Cross(normal,Vector3.up).normalized;
                        if(x.sqrMagnitude<.01f)x=Vector3.right;Vector3 y=Vector3.Cross(normal,x);
                        for(int j=0;j<48;j++){
                            float angle=j*2.399963f;Vector3 radial=x*Mathf.Cos(angle)+y*Mathf.Sin(angle);
                            Add(impactPoint+radial*(.26f+impact*.85f)+normal*impact*.35f,.027f+fade*.022f,colour*fade*.6f);GuardParticles++;
                        }
                    }
                    Add(impactPoint,.45f+impact*.8f,colour*fade*fade*.4f);
                    for(int j=0;j<detail*2;j++){
                        Vector3 d=Direction(j+173*i);
                        if(shot.blocked){Vector3 n=shot.impactNormal.sqrMagnitude>.01f?shot.impactNormal.normalized:(shot.origin-shot.target).normalized;d=(d-n*Vector3.Dot(d,n)).normalized+n*.20f;}
                        else d.y=Mathf.Abs(d.y)*1.2f;
                        Add(impactPoint+d*impact*(1.4f+Noise(j)*2.4f)+Vector3.down*impact*impact,.025f+fade*.035f,colour*fade);
                    }
                }
            }
            if(showing&&t>=63.16f&&t<67.9f){
                int n=quality==0?64:128;
                for(int j=0;j<n;j++){
                    float start=63.16f+(j%3)*.73f+Noise(j)*.35f,age=t-start;if(age<0||age>1.6f)continue;
                    var d=Direction(j);Vector3 origin=new Vector3(d.x*2.8f,6.8f+d.y*2.3f,26.65f);
                    Add(origin+new Vector3(d.x*1.4f,d.y*.8f,-.8f)*age+Vector3.down*age*age,.045f+Noise(j+11)*.08f,new Color(.46f,.58f,.7f)*(1-age/1.6f));
                }
            }
            particles.SetParticles(pool,count);
            particles.Simulate(0,false,false,false);
        }
    }
}
