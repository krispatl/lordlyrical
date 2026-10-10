using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace LordLyrical.Characters
{
    // Evaluated in the character's existing graph. Navigation/tracking owns the root.
    public sealed class HumanoidLocomotion
    {
        public readonly AnimationMixerPlayable Output;
        readonly HumanoidMotionLibrary library;
        readonly Quaternion motionFrame;
        public readonly Vector3 ModelForward;
        public readonly float ModelYaw;
        public static Vector3 Forward(Animator animator,Transform actor){
            var across=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position-animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
            var forward=actor.InverseTransformDirection(Vector3.Cross(actor.up,across).normalized);
            forward.y=0;return forward.sqrMagnitude>.1f?forward.normalized:Vector3.forward;
        }
        readonly AnimationClipPlayable[] players=new AnimationClipPlayable[19];
        readonly AnimationClip[] clips=new AnimationClip[19];
        public HumanoidLocomotion(PlayableGraph graph,HumanoidMotionLibrary source,Animator animator,Transform actor)
        {
            ModelForward=Forward(animator,actor);ModelYaw=Mathf.Atan2(ModelForward.x,ModelForward.z)*Mathf.Rad2Deg;motionFrame=Quaternion.Inverse(Quaternion.LookRotation(ModelForward,Vector3.up));
            library=source;Output=AnimationMixerPlayable.Create(graph,19);
            clips[0]=source.idle;
            for(int i=0;i<8;i++){clips[1+i]=source.walk[i];clips[9+i]=source.run[i];}
            clips[17]=source.turnLeft;clips[18]=source.turnRight;
            for(int i=0;i<19;i++){players[i]=AnimationClipPlayable.Create(graph,clips[i]?clips[i]:source.idle);players[i].SetSpeed(0);players[i].SetApplyFootIK(true);graph.Connect(players[i],0,Output,i);}
        }
        public void Sample(Vector3 localVelocity,float scale,float distance,float idleTime,float turnRate=0)
        {
            localVelocity=motionFrame*localVelocity;
            float speed=new Vector2(localVelocity.x,localVelocity.z).magnitude/Mathf.Max(.1f,scale);
            float moving=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.035f,.24f,speed));
            float running=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.3f,3.6f,speed));
            float turning=(1-moving)*Mathf.Clamp01((Mathf.Abs(turnRate)-25)/90)*.75f;
            for(int i=0;i<19;i++)Output.SetInputWeight(i,0);
            Output.SetInputWeight(0,1-moving-turning);
            Output.SetInputWeight(turnRate<0?17:18,turning);
            float angle=Mathf.Repeat(Mathf.Atan2(localVelocity.x,localVelocity.z)*Mathf.Rad2Deg,360)/45;
            int a=Mathf.FloorToInt(angle)%8,b=(a+1)%8;float f=angle-Mathf.Floor(angle);
            Output.SetInputWeight(1+a,moving*(1-running)*(1-f));Output.SetInputWeight(1+b,moving*(1-running)*f);
            Output.SetInputWeight(9+a,moving*running*(1-f));Output.SetInputWeight(9+b,moving*running*f);
            float stride=Mathf.Lerp(library.walkStride,library.runStride,running)*Mathf.Max(.1f,scale);
            float phase=Mathf.Repeat(distance/stride,1);
            for(int i=0;i<19;i++)players[i].SetTime(i>=1&&i<=16?phase*(clips[i]?clips[i].length:.8f):Mathf.Repeat(idleTime,clips[i]?clips[i].length:2.7f));
        }
        public static AvatarMask UpperBodyMask()
        {
            var mask=new AvatarMask();
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
            foreach(var p in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(p,true);
            return mask;
        }
    }
}
