using UnityEngine;
namespace LordLyrical
{
    // Signed shared clock continues beyond the song; musical cue times remain unchanged.
    public static class CinemaTiming
    {
        public const float IntroTitleFadeStart=-7,IntroTitleGone=-5;
        public const float ReturnStart=113.5f,RealmGone=118.2f;
        public const float EndTitleStart=124.25f,EndTitleFull=124.6f,EndTitleFadeStart=127,EndTitleGone=129;
        public const float FounderFadeStart=121.5f,FounderGone=124;
        public static float S(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        public static float Realm(float t)=>S(ReturnStart,RealmGone,t);
        public static float Founder(float t)=>S(FounderFadeStart,FounderGone,t);
        public static float TitleDissolve(float t)=>t<0?S(IntroTitleFadeStart,IntroTitleGone,t):S(EndTitleFadeStart,EndTitleGone,t);
        public static float Title(float t)=>t<0?.68f*(1-TitleDissolve(t)):S(EndTitleStart,EndTitleFull,t)*(1-TitleDissolve(t));
        public static float RoomEnergy(float t)=>t<9?(S(-4.5f,-2.2f,t)*.18f+S(-.85f,-.5f,t)*.82f)*(1-S(6.5f,8.6f,t)):S(114,116,t)*(1-S(FounderFadeStart,FounderGone,t));
        public static float Hover(float t)=>S(116,119,t)*(.30f+.025f*Mathf.Sin((t-119)*1.4f));
        public const float OutroComplete=EndTitleGone;
        public static bool Returning(float t)=>t>=ReturnStart;
    }
}
