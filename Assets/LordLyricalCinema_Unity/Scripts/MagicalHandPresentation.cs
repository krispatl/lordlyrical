using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands;
namespace LordLyrical
{
    // Cosmetic only: never modifies tracked joints, gesture thresholds or beam damage.
    public sealed class MagicalHandPresentation : MonoBehaviour
    {
        public const int Budget=96;
        ParticleSystem particles;readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[Budget];
        float charge42,previous42=-100;Material handMaterial,original;TrackedHandMesh hand;int count;
        public int ActiveParticles=>count;
        public void Initialize(TrackedHandMesh source)
        {
            hand=source;
            if(hand&&hand.skin){
                original=hand.skin.sharedMaterial;handMaterial=new Material(Resources.Load<Shader>("Cinema26/MagicHand"));
                if(original){if(original.HasProperty("_BaseMap"))handMaterial.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));else if(original.HasProperty("_MainTex"))handMaterial.SetTexture("_BaseMap",original.GetTexture("_MainTex"));
                    if(original.HasProperty("_BaseColor"))handMaterial.SetColor("_BaseColor",original.GetColor("_BaseColor"));}
                hand.skin.sharedMaterial=handMaterial;
            }
            var go=new GameObject("Hand | restrained resonance motes");go.transform.SetParent(transform,false);
            particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=particles.main;m.playOnAwake=false;m.loop=false;m.maxParticles=Budget;m.simulationSpace=ParticleSystemSimulationSpace.World;
            var e=particles.emission;e.enabled=false;var s=particles.shape;s.enabled=false;
            var r=particles.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Resources.Load<Material>("Concert/BeatAccents");r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.018f;
            particles.Play();particles.Pause();
        }
        static float N(int seed)=>CinemaPresentation.Hash(seed);
        void Add(Vector3 p,float alpha,float size,Color color)
        {
            if(count>=Budget||alpha<.002f)return;color.a=alpha;
            buffer[count++]=new ParticleSystem.Particle{position=p,startSize=size,startColor=color,startLifetime=1,remainingLifetime=1,randomSeed=(uint)(count+1)};
        }
        public void Render(float t,HandPoseInput.HandState pose,Color colour,bool remote=false)
        {
            if(!remote){var assigned=Shader.GetGlobalColor("_ParticipantTint");if(assigned.maxColorComponent>.01f)colour=assigned;}
            count=0;float energy=Magic33Score.Energy(t);float wanted=pose.tracked&&pose.open&&PalmBeamTiming.Allowed(t)?1:0;charge42=!pose.tracked?0:t<previous42||t-previous42>.5f?wanted:Mathf.MoveTowards(charge42,wanted,Mathf.Min(.05f,Time.unscaledDeltaTime)*(wanted>charge42?5:3));previous42=t;
            if(handMaterial)handMaterial.SetColor("_RootTint40",colour);
            if(handMaterial)handMaterial.SetFloat("_HandPower",pose.tracked?(Mathf.Max(StoryScore40.Awakening(t),pose.closed?.7f:charge42)):0);
            if(pose.tracked&&energy>0){
                int n=remote?12:40;Vector3 x=pose.rotation*Vector3.right,y=pose.rotation*Vector3.up;
                for(int i=0;i<n;i++){
                    float life=Mathf.Repeat(t*.55f+N(i*137),1),a=i*2.399963f+t*.55f;
                    Vector3 start=pose.position;
                    if(!remote&&hand&&hand.jointIds!=null){
                        int tip=(i%5);var id=tip==0?XRHandJointID.ThumbTip:tip==1?XRHandJointID.IndexTip:tip==2?XRHandJointID.MiddleTip:tip==3?XRHandJointID.RingTip:XRHandJointID.LittleTip;
                        for(int j=0;j<hand.jointIds.Length;j++)if(hand.jointIds[j]==id){start=hand.joints[j].position;break;}
                    }
                    if(charge42>.01f&&!pose.closed)start=Vector3.Lerp(pose.position,start,life);
                    Vector3 p=pose.closed?pose.position+(x*Mathf.Cos(a)+y*Mathf.Sin(a))*(.025f+.07f*(1-life)):start+Vector3.up*life*.08f+x*(N(i+39)-.5f)*.025f;
                    Add(p,energy*Mathf.Sin(life*Mathf.PI)*(pose.closed?.5f:.20f+charge42*.22f),.004f+N(i+13)*.004f,colour);
                }

            }
            particles.SetParticles(buffer,count);particles.Simulate(0,false,false,false);
        }
        public void Clear(){count=0;if(particles)particles.Clear();}
        void OnDisable()=>Clear();
        void OnDestroy(){if(hand&&hand.skin)hand.skin.sharedMaterial=original;if(handMaterial){if(Application.isPlaying)Destroy(handMaterial);else DestroyImmediate(handMaterial);}}
    }
}
