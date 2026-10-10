using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LordLyrical
{
    // Deterministic secondary animation on the preserved rig; no mesh or bind-pose replacement.
    public sealed class DragonPerformance
    {
        readonly Transform actor;
        readonly Transform[] nodes;
        readonly Dictionary<string,Transform> bones;
        readonly Vector3[] restPosition,restScale,blendPosition;
        readonly Quaternion[] restRotation,blendRotation;
        readonly AnimationClip unfold,fly,roar;
        readonly Transform head,jaw;
        readonly Transform[] aimChain;
        readonly Quaternion[] aimRest=new Quaternion[5];
        static readonly float[] aimWeights={.13f,.18f,.22f,.22f,.25f};
        // Lip sockets in the preserved rig's bone coordinates, derived from the
        // authored front teeth after the original head refinement. Bone +Y follows
        // the snout and +Z points toward the lower jaw; scale follows the actor.
        static readonly Vector3 upperLip=new Vector3(0,1.062f,.090f);
        static readonly Vector3 lowerLip=new Vector3(0,1.132f,-.067f);
        public Vector3 Mouth=>head&&jaw?(head.TransformPoint(upperLip)+jaw.TransformPoint(lowerLip))*.5f:actor.position;
        public Vector3 MouthForward=>head&&jaw?(head.up+jaw.up).normalized:actor.forward;
        void AimAt(Vector3 target,float weight)
        {
            if(weight<=0||!head)return;
            for(int i=0;i<aimChain.Length;i++)aimRest[i]=aimChain[i].localRotation;
            for(int pass=0;pass<4;pass++){
                var aim=Quaternion.FromToRotation(MouthForward,(target-Mouth).normalized);
                aim.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                if(Mathf.Abs(angle)<.001f)break;angle=Mathf.Clamp(angle,-65,65);
                for(int i=0;i<aimChain.Length;i++)aimChain[i].rotation=Quaternion.AngleAxis(angle*aimWeights[i],axis)*aimChain[i].rotation;
            }
            for(int i=0;i<aimChain.Length;i++)aimChain[i].localRotation=Quaternion.Slerp(aimRest[i],aimChain[i].localRotation,weight);
        }
        void AimBreath(float time,Vector3 target)
        {
            float weight=S(68.2f,69,time)*(1-S(81,82.2f,time));
            if(weight<=0||!head||!jaw)return;
            // Keep a readable aperture through the sustained breath while preserving
            // the wider authored roar. Rotation about head +X opens toward its +Z.
            float opening=Vector3.SignedAngle(head.up,jaw.up,head.right);
            jaw.rotation=Quaternion.AngleAxis(Mathf.Max(0,22-opening)*weight,head.right)*jaw.rotation;
            for(int i=0;i<aimChain.Length;i++)aimRest[i]=aimChain[i].localRotation;
            // A bounded deterministic solve distributes the look across the whole
            // neck. Recompute after each pass because bending also moves the lips.
            for(int pass=0;pass<4;pass++){
                var aim=Quaternion.FromToRotation(MouthForward,(target-Mouth).normalized);
                aim.ToAngleAxis(out float angle,out Vector3 axis);
                if(angle>180)angle-=360;
                if(Mathf.Abs(angle)<.001f)break;
                angle=Mathf.Clamp(angle,-75,75);
                for(int i=0;i<aimChain.Length;i++)aimChain[i].rotation=Quaternion.AngleAxis(angle*aimWeights[i],axis)*aimChain[i].rotation;
            }
            for(int i=0;i<aimChain.Length;i++)aimChain[i].localRotation=Quaternion.Slerp(aimRest[i],aimChain[i].localRotation,weight);
        }
        public DragonPerformance(Transform actor,AnimationClip unfold,AnimationClip fly,AnimationClip roar)
        {
            this.actor=actor;this.unfold=unfold;this.fly=fly;this.roar=roar;
            nodes=actor.GetComponentsInChildren<Transform>(true).Where(t=>t!=actor).ToArray();
            bones=nodes.GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            bones.TryGetValue("head",out head);bones.TryGetValue("jaw",out jaw);
            aimChain=new[]{"neck_01","neck_02","neck_03","neck_04","head"}.Select(name=>bones[name]).ToArray();
            restPosition=nodes.Select(t=>t.localPosition).ToArray();
            restRotation=nodes.Select(t=>t.localRotation).ToArray();
            restScale=nodes.Select(t=>t.localScale).ToArray();
            blendPosition=new Vector3[nodes.Length];blendRotation=new Quaternion[nodes.Length];
        }
        static float S(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        static float Window(float a,float b,float c,float d,float t)=>S(a,b,t)*(1-S(c,d,t));
        // Integrate the charge envelope so the wing clock slows continuously.
        // Releasing the charge preserves phase instead of racing back to wall time.
        static float SmoothIntegral(float a,float b,float t)
        {
            float duration=b-a,u=Mathf.Clamp01((t-a)/duration);
            return duration*(u*u*u-.5f*u*u*u*u)+Mathf.Max(0,t-b);
        }
        public static float FlightClock(float t)=>t-.8f*(SmoothIntegral(94.5f,96,t)-SmoothIntegral(97.4f,98,t));
        void Reset()
        {
            for(int i=0;i<nodes.Length;i++){nodes[i].localPosition=restPosition[i];nodes[i].localRotation=restRotation[i];nodes[i].localScale=restScale[i];}
        }
        void Sample(AnimationClip clip,float time)
        {
            if(clip)clip.SampleAnimation(actor.gameObject,Mathf.Clamp(time,0,clip.length-.0001f));
        }
        void Blend(AnimationClip clip,float time,float weight)
        {
            if(!clip||weight<=0)return;
            for(int i=0;i<nodes.Length;i++){blendPosition[i]=nodes[i].localPosition;blendRotation[i]=nodes[i].localRotation;}
            Sample(clip,time);
            for(int i=0;i<nodes.Length;i++){
                nodes[i].localPosition=Vector3.Lerp(blendPosition[i],nodes[i].localPosition,weight);
                nodes[i].localRotation=Quaternion.Slerp(blendRotation[i],nodes[i].localRotation,weight);
                nodes[i].localScale=restScale[i];
            }
        }
        void Turn(string name,float pitch,float yaw,float roll)
        {
            if(!bones.TryGetValue(name,out var bone))return;
            bone.rotation=Quaternion.AngleAxis(yaw,actor.up)*Quaternion.AngleAxis(pitch,actor.right)*Quaternion.AngleAxis(roll,actor.forward)*bone.rotation;
        }
        // Powerful downstroke occupies 35% of a beat; the folded recovery takes the rest.
        static float WingCycle(float cycle)
        {
            float p=Mathf.Repeat(cycle,1);
            return p<.35f?Mathf.Lerp(1,-1,S(0,.35f,p)):Mathf.Lerp(-1,1,S(.35f,1,p));
        }
        public void SampleShow(float t,Vector3 anchor,float scale,Vector3? breathTarget=null,float support=0,DragonShot[] shots=null,float playerReaction=0)
        {
            float stalk=Window(62.0f,62.95f,65.4f,66.15f,t);
            float burst=Window(64.35f,64.68f,65.05f,66.1f,t);
            float open=S(63,68.5f,t),flight=S(67.5f,69.5f,t),fade=S(98,101,t);
            float charge=Window(94.5f,96,97.4f,98,t);
            float clock=FlightClock(t);
            float cycle=(clock-68.2f)/2.35f;
            float beat=WingCycle(cycle);
            float breath=Mathf.Sin(t*1.35f);
            float revealRoar=DragonRevealScore.RoarWeight(t);
            float roarWeight=Mathf.Max(revealRoar,Window(73.4f,74.1f,77.7f,78.5f,t));
            float recoil=Mathf.Max(Window(90,90.18f,90.65f,92.3f,t),BattleScore.Recoil(t)*.85f)+Window(80.8f,81.3f,81.7f,83,t)*support*.5f;
            float sweep=Mathf.Sin((t-69)*.25f);
            float evade=Window(81.1f,82,82.9f,84.4f,t);
            float challenge=Window(85.0f,86.2f,87.1f,88.5f,t);
            float counterShock=Window(90,90.24f,90.6f,92.3f,t);
            float finishingHit=Window(98,98.14f,98.3f,99.3f,t);
            float impactKick=HeroDuelScore.FollowThrough(t)*.8f+playerReaction*.45f;
            float weightShift=evade*.7f-challenge*.45f;
            float bank=flight*Mathf.Sin((t-69)*.25f+.7f)*2.5f+evade*7-challenge*4;

            actor.localScale=Vector3.one*scale;
            actor.localPosition=anchor+new Vector3(flight*sweep*.85f+weightShift,
                (1-DragonRevealScore.Detach(t))*.35f-stalk*.65f+(1-open)*.2f+flight*(.3f+.18f*WingCycle(cycle-.1f))+roarWeight*.15f+evade*.32f-challenge*.16f-recoil*.18f-fade*4,
                (1-S(64.15f,66.15f,t))*9.0f-burst*.65f+flight*(4.25f-.6f*Mathf.Sin((t-69)*.18f))+recoil*.35f+impactKick*.24f+fade*.4f);
            actor.localRotation=Quaternion.Euler(stalk*38+flight*(20*(1-fade)+2.2f*beat-recoil*5),180+flight*sweep*8,bank);
            // The final music cue owns destruction, regardless of group charge.
            actor.localRotation*=Quaternion.Euler(-fade*16-finishingHit*12,finishingHit*5,fade*11+finishingHit*8);
            Reset();
            if(flight>=1&&fly)Sample(fly,Mathf.Repeat(cycle,1)*fly.length);
            else {
                Sample(unfold,DragonRevealScore.Unfold(t)*(unfold?unfold.length:6));
                if(fly)Blend(fly,Mathf.Repeat(cycle,1)*fly.length,flight);
            }
            if(roar)Blend(roar,(t<69?S(DragonRevealScore.Roar,68.7f,t):S(73.5f,78,t))*roar.length,roarWeight*.9f);
            float wingGain=flight*(1-roarWeight*.35f)*(1-charge*.65f)*(1-fade*.85f);
            Turn("chest",breath*.65f-recoil*5+challenge*3+stalk*3-burst*8,weightShift*3+impactKick*3,flight*Mathf.Sin(t*.7f)*.7f-counterShock*2-impactKick*2);
            Turn("neck_01",-roarWeight*5+recoil*8+HeroDuelScore.FollowThrough(t-.10f)*3,flight*Mathf.Sin(t*.4f)*2,0);
            Turn("neck_02",-roarWeight*3+recoil*8+impactKick*5+Window(98.08f,98.3f,98.6f,99.5f,t)*12,flight*Mathf.Sin(t*.4f-.3f)*2.5f,0);
            Turn("neck_03",roarWeight*5+breath*.5f,flight*Mathf.Sin(t*.4f-.6f)*2.5f,0);
            Turn("neck_04",roarWeight*4,flight*Mathf.Sin(t*.4f-.85f)*2,0);
            Turn("head",breath*.8f-recoil*11-HeroDuelScore.FollowThrough(t-.18f)*7-Window(98.18f,98.5f,98.8f,99.7f,t)*14,flight*Mathf.Sin(t*.4f-1)*3,0);
            Turn("jaw",flight*(2+2*Mathf.Sin(t*1.35f-.4f))*(1-roarWeight),0,0);
            foreach(string side in new[]{"L","R"}){
                float sign=side=="L"?-1:1;
                float asymmetry=1+sign*bank*.018f;
                float impactWing=HeroDuelScore.FollowThrough(t-.13f)*.65f+playerReaction*.3f;
                float collapse=S(98.15f,100.6f,t);
                Turn("wing_upper."+side,0,sign*wingGain*Mathf.Sin(cycle*Mathf.PI*2)*3,sign*(wingGain*beat*34*asymmetry+impactWing*4-collapse*16));
                Turn("wing_fore."+side,0,sign*wingGain*4,sign*(wingGain*WingCycle(cycle-.09f)*16*asymmetry+HeroDuelScore.FollowThrough(t-.23f)*3-collapse*9));
                for(int finger=1;finger<=4;finger++){
                    float delayed=WingCycle(cycle-.13f-finger*.012f);
                    Turn("wing_finger"+finger+"_a."+side,wingGain*delayed*1.8f,0,sign*wingGain*delayed*(1.6f+finger*.45f));
                    Turn("wing_finger"+finger+"_b."+side,wingGain*delayed*2.4f,0,sign*wingGain*Mathf.Sin(cycle*6.283f-finger*.3f)*1.8f);
                }
                Turn("fore_upper."+side,flight*(10+beat*2)+impactKick*7+roarWeight*6+evade*(side=="L"?9:3),0,sign*flight*3);
                Turn("fore_lower."+side,-flight*14+flight*WingCycle(cycle-.15f)*3,0,0);
                Turn("hind_upper."+side,flight*(-8+beat*2)-evade*4+challenge*5,0,sign*flight*2);
                Turn("hind_lower."+side,flight*12,0,0);
                Turn("hind_ankle."+side,-flight*7,0,0);
                for(int toe=1;toe<=3;toe++)Turn("fore_foot_toe"+toe+"."+side,flight*(5+2*Mathf.Sin(t*1.1f-toe*.3f)),0,0);
            }
            var conductor=breathTarget??DragonBreath.Target(t,ConductorRoute.Position(t));
            // Head leads the breach; the neck stays pitched down toward JERU.
            AimAt(conductor,Mathf.Max(stalk,Window(68.2f,69,97.2f,98,t)*.75f));
            AimBreath(t,conductor);
            if(shots!=null)for(int i=0;i<shots.Length;i++)if(shots[i].armed){
                float chargeShot=DragonFireballScore.Charge(t,i);
                // Eyes/head settle onto the authority's locked target before the jaw opens.
                float intent=DragonFireballScore.Aim(t,i);
                Turn("neck_02",-intent*2.5f,0,0);Turn("neck_04",intent*3,0,0);
                Turn("wing_upper.L",0,0,-chargeShot*3);Turn("wing_upper.R",0,0,chargeShot*3);
                Turn("chest",-chargeShot*4,0,0);Turn("jaw",chargeShot*12,0,0);
                Turn("fore_upper.L",chargeShot*4,0,-chargeShot*2);Turn("fore_upper.R",chargeShot*4,0,chargeShot*2);
                AimAt(shots[i].target,intent);
                float kick=Window(DragonFireballScore.Launch(i),DragonFireballScore.Launch(i)+.10f,DragonFireballScore.Launch(i)+.16f,DragonFireballScore.Launch(i)+.5f,t);
                Turn("neck_01",kick*6,0,0);Turn("head",-kick*3,0,0);
            }
            for(int i=1;i<=13;i++){
                float delayed=clock*1.25f-i*.42f;
                float weight=flight*(1-charge*.75f);
                Turn("tail_"+i.ToString("00"),weight*Mathf.Sin(delayed-.5f)*1.5f,
                    weight*Mathf.Sin(delayed)*(1.5f+i*.16f)+recoil*Mathf.Sin((t-90)*5-i*.3f)*2.5f-weightShift*(1+i*.10f)+HeroDuelScore.FollowThrough(t-i*.045f)*Mathf.Sin(i*.4f)*3+playerReaction*Mathf.Sin(i*.4f)*1.6f,0);
            }
        }
    }
}
