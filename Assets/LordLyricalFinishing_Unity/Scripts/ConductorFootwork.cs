using UnityEngine;
namespace LordLyrical
{
    // Planted stance targets are reconstructed analytically, rather than accumulated frame to frame.
    public sealed class ConductorFootwork
    {
        readonly Animator rig;readonly Transform actor;
        readonly Vector3 leftOffset,rightOffset;
        public ConductorFootwork(Animator animator,Transform root)
        {rig=animator;actor=root;leftOffset=actor.InverseTransformPoint(rig.GetBoneTransform(HumanBodyBones.LeftFoot).position);rightOffset=actor.InverseTransformPoint(rig.GetBoneTransform(HumanBodyBones.RightFoot).position);}
        public static Vector3 Position(float t)
        {float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(69,70,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(88,90,t)));return new Vector3(Mathf.Sin((t-69)*.5f)*3*blend,0,5+Mathf.Cos((t-69)*.5f)*blend);}
        public void Evaluate(float t)
        {
            if(t<69||t>=90)return;
            Step(t,true,leftOffset);Step(t,false,rightOffset);
        }
        void Step(float t,bool left,Vector3 offset)
        {
            const float period=.92f;float phaseOffset=left?0:period*.5f;
            float cycle=Mathf.Floor((t-69+phaseOffset)/period)*period+69-phaseOffset;
            float phase=(t-cycle)/period;
            Vector3 a=Position(Mathf.Clamp(cycle+.2f,69,90)),b=Position(Mathf.Clamp(cycle+period+.2f,69,90));
            Vector3 target=a+actor.TransformVector(offset);
            if(phase>.58f){float u=(phase-.58f)/.42f;target=Vector3.Lerp(a,b,Mathf.SmoothStep(0,1,u))+actor.TransformVector(offset)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.13f;}
            Solve(rig.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg),rig.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg),rig.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot),target,actor.forward);
        }
        static void Solve(Transform hip,Transform knee,Transform foot,Vector3 target,Vector3 hint)
        {
            Quaternion footRotation=foot.rotation;Vector3 delta=target-hip.position;float a=Vector3.Distance(hip.position,knee.position),b=Vector3.Distance(knee.position,foot.position),d=Mathf.Clamp(delta.magnitude,.001f,a+b-.001f);Vector3 direction=delta.normalized;
            float projection=(a*a+d*d-b*b)/(2*d);Vector3 bend=Vector3.ProjectOnPlane(hint,direction).normalized;Vector3 wantedKnee=hip.position+direction*projection+bend*Mathf.Sqrt(Mathf.Max(0,a*a-projection*projection));
            hip.rotation=Quaternion.FromToRotation(knee.position-hip.position,wantedKnee-hip.position)*hip.rotation;
            knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,target-knee.position)*knee.rotation;foot.rotation=footRotation;
        }
    }
}
