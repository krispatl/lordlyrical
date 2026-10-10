using UnityEngine;
namespace LordLyrical
{
    public enum OrcAttack { Slam,Sweep,Double,Thrust,Burst }
    public static class OrcCombatMotion
    {
        public static float Windup(OrcAttack a)=>a==OrcAttack.Burst?1.05f:a==OrcAttack.Thrust?.9f:.8f;
        public static float Swing(OrcAttack a)=>a==OrcAttack.Double?.8f:.48f;
        public static float Recover(OrcAttack a)=>a==OrcAttack.Slam?.85f:.65f;
        public static float Duration(OrcAttack a)=>Windup(a)+Swing(a)+Recover(a);
        public static int Clip(OrcAttack a)=>a switch{OrcAttack.Sweep=>6,OrcAttack.Double=>7,OrcAttack.Thrust=>8,OrcAttack.Burst=>9,_=>2};
        public static float SourceDuration(OrcAttack a)=>a==OrcAttack.Double||a==OrcAttack.Burst?1:2f/3;
        public static float NormalizedTime(OrcAttack a,float age)
        {
            float windup=Windup(a),swing=Swing(a);
            if(age<windup)return Mathf.Lerp(0,.25f,Mathf.SmoothStep(0,1,age/windup));
            if(age<windup+swing)return Mathf.Lerp(.25f,.82f,(age-windup)/swing);
            return Mathf.Lerp(.82f,1,Mathf.Clamp01((age-windup-swing)/Recover(a)));
        }
        public static float ContactTime(OrcAttack a,int index)=>Windup(a)+Swing(a)*(a==OrcAttack.Double?(index==0?.23f:.75f):.48f);
        public static int ContactCount(OrcAttack a)=>a==OrcAttack.Double?2:1;
        public static float ContactRange(OrcAttack a)=>a==OrcAttack.Burst?3.2f:a==OrcAttack.Thrust?2.7f:2.45f;
        // These volumes match the visible resonance arc; the solid weapon never moves a user.
        public static bool Contains(OrcAttack a,Vector3 origin,float yaw,Vector3 head)
        {
            Vector3 local=Quaternion.Inverse(Quaternion.Euler(0,yaw,0))*(head-origin);local.y=0;
            if(local.magnitude>ContactRange(a)||local.z<.2f)return false;
            float angle=Mathf.Abs(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg);
            return angle<(a==OrcAttack.Sweep||a==OrcAttack.Double?65:a==OrcAttack.Burst?48:32);
        }
    }
}
