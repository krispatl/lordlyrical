using UnityEngine;
namespace LordLyrical
{
    public static class Combat34Score
    {
        public static float FireballWarning(float t,int index,DragonShot shot)
        {
            if(shot==null||!shot.armed||shot.resolved)return 0;
            float launch=DragonFireballScore.Launch(index),age=t-launch;
            // Mono may retain excess precision in the sum but round the caller's
            // timestamp. Ten microseconds is below a frame and keeps the edge stable.
            if(t<launch-DragonFireballScore.Windup||t>=launch+DragonFireballScore.Flight-.00001f)return 0;
            float prepare=CinemaTiming.S(-DragonFireballScore.Windup,-DragonFireballScore.Windup+.2f,age);
            return prepare*Mathf.Lerp(.35f,1,Mathf.InverseLerp(-DragonFireballScore.Windup,DragonFireballScore.Flight,age));
        }
        public static float OrcWarning(ConcertOrc orc,float t)
        {
            if(orc==null||orc.health<=0)return 0;
            if(orc.intent==OrcIntent.Strike)return 1;
            if(orc.intent!=OrcIntent.Telegraph)return 0;
            return Mathf.Lerp(.25f,.9f,CinemaTiming.S(0,OrcCombatMotion.Windup(orc.attack),t-orc.attackStarted));
        }
        public static float GuardEnvelope(float age)=>BattleScore.W(0,.05f,.12f,.60f,age);
    }
}
