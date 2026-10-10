using System.Collections.Generic;
using UnityEngine;
namespace LordLyrical.Characters
{
    public sealed class OrcPhysicalPerformance
    {
        readonly Transform actor,pelvis,chest,ru,rl,rh,lu,ll,lh,rightSocket,leftSocket;
        readonly Vector3 hipRest;
        public OrcPhysicalPerformance(Transform actor,Dictionary<string,Transform> bones,Transform rightSocket,Transform leftSocket)
        {
            this.actor=actor;this.rightSocket=rightSocket;this.leftSocket=leftSocket;
            bones.TryGetValue("pelvis",out pelvis);bones.TryGetValue("spine_03",out chest);
            bones.TryGetValue("upperarm_r",out ru);bones.TryGetValue("lowerarm_r",out rl);bones.TryGetValue("hand_r",out rh);
            bones.TryGetValue("upperarm_l",out lu);bones.TryGetValue("lowerarm_l",out ll);bones.TryGetValue("hand_l",out lh);
            hipRest=pelvis?actor.InverseTransformPoint(pelvis.position):Vector3.up*1.4f;
        }
        static float S(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        public void Recoil(ConcertOrc o,float t){
            float death=Presentation43Score.DeathLean(t,o);
            if(chest&&death>0){Vector3 d=actor.InverseTransformDirection(Presentation43Score.DeathDirection(o));chest.rotation=Quaternion.AngleAxis(d.z*12*death,actor.right)*Quaternion.AngleAxis(-d.x*10*death,actor.forward)*chest.rotation;}
            float weight=ImpactReaction37.Weight(t,o);if(weight<=0||!chest)return;
            Vector3 away=actor.InverseTransformDirection(-o.reactionNormal);
            if(pelvis)pelvis.position+=actor.TransformDirection(new Vector3(away.x*.035f,-.055f,away.z*.035f))*weight;
            chest.rotation=Quaternion.AngleAxis(away.z*14*weight,actor.right)*Quaternion.AngleAxis(-away.x*11*weight,actor.forward)*chest.rotation;
        }
        public void Sample(int clip,float time,float length)
        {
            if(pelvis){
                // Navigation owns travel. Keep the source's weight shifts without its authored root drift.
                var p=actor.InverseTransformPoint(pelvis.position);var drift=p-hipRest;drift.y=0;
                var limited=Vector3.ClampMagnitude(drift,clip==4?.65f:clip==1||clip==5?.18f:.32f);
                pelvis.position+=actor.TransformVector(limited-drift);
            }
            if(clip!=2||!rh||!rightSocket)return;
            float p01=Mathf.Clamp01(time/Mathf.Max(.01f,length));
            float influence=S(0,.12f,p01)*(1-S(.82f,1,p01));
            float load=S(0,.25f,p01),strike=S(.27f,.525f,p01);
            float recovery=S(.58f,.96f,p01);
            if(chest)chest.rotation=Quaternion.AngleAxis(Mathf.Lerp(-13*load,17,strike)*(1-recovery)*influence,actor.right)*chest.rotation;
            Vector3 ready=new Vector3(.48f,1.75f,.3f),raised=new Vector3(.25f,2.8f,-.08f),impact=new Vector3(.18f,.98f,1.12f);
            Vector3 target=Vector3.Lerp(ready,raised,load);target=Vector3.Lerp(target,impact,strike);target=Vector3.Lerp(target,ready,recovery);
            Vector3 world=Vector3.Lerp(rh.position,actor.TransformPoint(target),influence);
            AnatomicalIK.Solve(ru,rl,rh,world,actor.right*.65f-actor.forward*.4f);
            Vector3 shaft=Vector3.Slerp(Vector3.up,new Vector3(.05f,-.48f,1).normalized,strike*(1-recovery));
            Vector3 desired=actor.TransformDirection(shaft);
            rh.rotation=Quaternion.Slerp(rh.rotation,Quaternion.FromToRotation(rightSocket.up,desired)*rh.rotation,influence);
            if(lh&&leftSocket){
                Vector3 support=rightSocket.position-desired*.25f;
                AnatomicalIK.Solve(lu,ll,lh,Vector3.Lerp(lh.position,support,influence),-actor.right*.6f-actor.forward*.3f);
                lh.rotation=Quaternion.Slerp(lh.rotation,Quaternion.FromToRotation(leftSocket.up,desired)*lh.rotation,influence);
            }
        }
    }
}
