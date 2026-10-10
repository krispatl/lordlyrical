using UnityEngine;
namespace LordLyrical
{
    // One target for the skeletal aim, ribbon core and particles. The sweep begins
    // on its existing 74-second cue and blends out of the conductor strike.
    public static class DragonBreath
    {
        // All visible breath layers share the established particle attack/release.
        public static float Strength(float time)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(69,69.2f,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(80.4f,81,time)));
        public static float ChargeStrength(float time)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(68.2f,69,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(69,69.2f,time)));
        public static float CounterStrength(float time)=>Strength(time)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(76,76.3f,time));
        public static bool TryWardContact(Vector3 mouth,Vector3 target,Vector3 center,float radius,out Vector3 contact)
        {
            contact=Vector3.zero;var offset=mouth-center;var segment=target-mouth;float length=segment.magnitude;
            if(length<.0001f||radius<=0)return false;
            var direction=segment/length;float b=Vector3.Dot(offset,direction),c=offset.sqrMagnitude-radius*radius,discriminant=b*b-c;
            if(discriminant<0)return false;
            float root=Mathf.Sqrt(discriminant),distance=-b-root;if(distance<0)distance=-b+root;
            if(distance<0||distance>length)return false;
            contact=mouth+direction*distance;return contact.y>=center.y;
        }
        public static float SweepBlend(float time)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(74,74.6f,time));
        public static Vector3 Target(float time,Vector3 conductor)
        {
            return BattleScore.ShieldPoint(conductor)+new Vector3(Mathf.Sin((time-69)*1.1f)*.12f,Mathf.Sin((time-69)*.7f)*.08f,0);
        }
    }
}
