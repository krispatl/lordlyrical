using UnityEngine;
namespace LordLyrical
{
    // Absolute narrative time: anticipation precedes the existing musical strike cues.
    public static class HeroDuelScore
    {
        static readonly float[] strikes={71.85f,76.35f,85.3f,90.25f,98f};
        public static float Anticipation(float t)
        {
            float result=0;
            foreach(float hit in strikes)result=Mathf.Max(result,BattleScore.W(hit-.80f,hit-.42f,hit-.22f,hit,t));
            return result;
        }
        public static float FollowThrough(float t)
        {
            float result=0;
            foreach(float hit in strikes)result=Mathf.Max(result,BattleScore.W(hit,hit+.16f,hit+.30f,hit+.78f,t));
            return result;
        }
        public static float PlayerReaction(float t,ConcertState state)
        {
            if(state==null||state.dragonHitSerial<=0||t<92||t>=100)return 0;
            float age=t-state.dragonHitAt;
            return BattleScore.W(0,.10f,.18f,.60f,age);
        }
        public static float ImpactAge(float t)
        {
            float age=100;
            foreach(float hit in strikes)if(t>=hit)age=Mathf.Min(age,t-hit);
            return age;
        }
        public static int StrikeIndex(float t){int best=0;float distance=100;for(int i=0;i<strikes.Length;i++){float d=Mathf.Abs(t-strikes[i]);if(d<distance){distance=d;best=i;}}return best;}
        public static float Style(float t)=>Mathf.Lerp(-1,1,StrikeIndex(t)/4f);
        public static float RightExtension(float t)=>Mathf.Clamp(.43f-.12f*Anticipation(t)+.44f*BattleScore.Counter(t),.30f,.90f);
        public static float Guard(float t)=>BattleScore.W(67,68.8f,99.8f,103,t);
        public static float FinalGather(float t)=>BattleScore.W(92,94,97.6f,98,t);
        public static float FinalRelease(float t)=>BattleScore.W(97.7f,98.05f,98.3f,100.4f,t);
    }
}
