using UnityEngine;
namespace LordLyrical
{
    public static class DragonRevealScore
    {
        public const float Start=63,Wings=64.673f,Eyes=65.152f,Roar=66.309f,Attack=69;
        public static float S(float a,float b,float t)=>BattleScore.S(a,b,t);
        public static float Detach(float t)=>S(Start+.15f,Wings+.45f,t);
        public static float Unfold(float t)=>.08f*S(Start,Wings,t)+.67f*S(Wings,Wings+.55f,t)+.25f*S(Wings+.55f,68.5f,t);
        public static float Presence(float t)=>S(Start,Start+.15f,t);
        public static float Light(float t)=>Mathf.Lerp(.42f,1,S(Wings,68.8f,t));
        public static float EyeLight(float t)=>Mathf.Lerp(.28f,1,S(Eyes,Eyes+.20f,t));
        public static float RoarWeight(float t)=>BattleScore.W(Roar,Roar+.30f,67.8f,68.7f,t);
        public static float Rib(float t,float z)=>S(Start+Mathf.Clamp01((z+4)/24)*.65f,Wings+.65f,t);
        public static float Glyph(float t,float z)=>BattleScore.W(Start+Mathf.Clamp01((z+4)/24)*.65f,Start+.85f+Mathf.Clamp01((z+4)/24)*.65f,66.4f,68,t);
    }
}
