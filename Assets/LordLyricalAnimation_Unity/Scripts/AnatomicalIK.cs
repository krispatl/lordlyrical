using UnityEngine;
namespace LordLyrical.Characters
{
    public static class AnatomicalIK
    {
        public static Vector3 PreserveBend(Transform upper,Transform lower,Transform end,Vector3 fallback)
        {
            if(!upper||!lower||!end)return fallback;
            Vector3 axis=(end.position-upper.position).normalized;
            Vector3 pole=Vector3.ProjectOnPlane(lower.position-upper.position,axis);
            return pole.sqrMagnitude>.000001f?pole.normalized:fallback;
        }
        public static void Solve(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 hint)
        {
            if(!upper||!lower||!end)return;
            Vector3 origin=upper.position,delta=target-origin;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
            if(a<.001f||b<.001f||delta.sqrMagnitude<.000001f)return;
            float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(hint,direction).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
            float along=(a*a-b*b+d*d)/(2*d);Vector3 elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+direction*d-lower.position)*lower.rotation;
        }
    }
    public sealed class GroundedFeet
    {
        readonly Transform[] upper=new Transform[2],lower=new Transform[2],feet=new Transform[2];
        readonly float[] sole=new float[2],lockAge=new float[2],contactWeight=new float[2];readonly bool[] released=new bool[2];readonly Vector3[] planted=new Vector3[2];
        readonly bool[] locked=new bool[2];readonly Vector3[] restPole=new Vector3[2];readonly Transform root;
        public GroundedFeet(Animator animator,Transform actor)
        {
            root=actor;
            for(int i=0;i<2;i++){
                upper[i]=animator.GetBoneTransform(i==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                lower[i]=animator.GetBoneTransform(i==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                feet[i]=animator.GetBoneTransform(i==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                sole[i]=Mathf.Clamp(root.InverseTransformPoint(feet[i].position).y,.03f,.2f);
                var toe=animator.GetBoneTransform(i==0?HumanBodyBones.LeftToes:HumanBodyBones.RightToes);
                Vector3 forward=toe?Vector3.ProjectOnPlane(toe.position-feet[i].position,root.up).normalized:root.forward;
                restPole[i]=root.InverseTransformDirection(AnatomicalIK.PreserveBend(upper[i],lower[i],feet[i],forward));
            }
        }
        public GroundedFeet(Transform actor,Transform leftUpper,Transform leftLower,Transform leftFoot,Transform rightUpper,Transform rightLower,Transform rightFoot)
        {
            root=actor;upper[0]=leftUpper;lower[0]=leftLower;feet[0]=leftFoot;upper[1]=rightUpper;lower[1]=rightLower;feet[1]=rightFoot;
            for(int i=0;i<2;i++){
                sole[i]=feet[i]?Mathf.Clamp(root.InverseTransformPoint(feet[i].position).y,.03f,.25f):.19f;
                restPole[i]=root.InverseTransformDirection(AnatomicalIK.PreserveBend(upper[i],lower[i],feet[i],root.forward));
            }
        }
        public void Reset(){locked[0]=locked[1]=released[0]=released[1]=false;lockAge[0]=lockAge[1]=contactWeight[0]=contactWeight[1]=0;}
        public void Evaluate(float floor,float speed,float delta,bool reset=false)
        {
            if(reset)Reset();
            for(int i=0;i<2;i++){
                var foot=feet[i];if(!foot)continue;
                Vector3 p=foot.position;float ground=floor+sole[i]*root.lossyScale.y;
                float lift=p.y-ground;
                // Blend contact in/out; a released foot must swing before locking again.
                // Reacquiring on the same low-foot frame used to pop the planted target.
                if(released[i]&&(lift>.05f*root.lossyScale.y||speed<.08f))released[i]=false;
                if(locked[i]){
                    lockAge[i]+=delta;
                    if(lift>.075f*root.lossyScale.y||lockAge[i]>.38f||Vector3.Distance(p,planted[i])>.3f||speed<.08f){locked[i]=false;released[i]=true;}
                }
                if(!locked[i]&&!released[i]&&contactWeight[i]<.001f&&lift<.028f*root.lossyScale.y&&speed>.12f){locked[i]=true;lockAge[i]=0;planted[i]=new Vector3(p.x,ground,p.z);}
                contactWeight[i]=Mathf.MoveTowards(contactWeight[i],locked[i]?.85f:0,Mathf.Max(0,delta)*8.5f);
                Vector3 target=Vector3.Lerp(p,planted[i],contactWeight[i]);target.y=Mathf.Max(target.y,ground);
                if((target-p).sqrMagnitude>.00000001f){
                    Vector3 pole=AnatomicalIK.PreserveBend(upper[i],lower[i],foot,root.TransformDirection(restPole[i]));
                    Quaternion rotation=foot.rotation;AnatomicalIK.Solve(upper[i],lower[i],foot,target,pole);foot.rotation=rotation;
                }
            }
        }
    }
}
