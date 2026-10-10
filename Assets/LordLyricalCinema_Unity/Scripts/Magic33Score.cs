using UnityEngine;
namespace LordLyrical
{
    public static class Magic33Score
    {
        public static float Clock {get;private set;}=-1000;
        public static float Energy(float t)=>CinemaTiming.S(-4.5f,-2,t)*(1-CinemaTiming.S(CinemaTiming.FounderFadeStart,CinemaTiming.FounderGone,t));
        public static float PortalOffering(float t)=>BattleScore.W(.5f,1.5f,6,8,t);
        public static float Quiet(float t)=>1-.7f*BattleScore.W(96,97,97.75f,98,t);
        public static float SurfaceWave(float t,float height)
        {
            float value=0;float delay=Mathf.Clamp(height,0,12)*.09f;
            foreach(float cue in Cues)value=Mathf.Max(value,BattleScore.W(cue+delay,cue+delay+.18f,cue+delay+.3f,cue+delay+.85f,t));
            return value;
        }
        static readonly float[] Cues={21,39,52,59,90,98,101};
        public static void SetClock(float t,bool waiting)
        {
            Clock=waiting?-1000:t;Shader.SetGlobalFloat("_MagicClock",waiting?0:t);
            Shader.SetGlobalFloat("_MagicEnergy",waiting?0:Energy(t));
        }
    }
}
