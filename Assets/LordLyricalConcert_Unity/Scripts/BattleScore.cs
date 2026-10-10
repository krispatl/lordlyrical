using UnityEngine;
namespace LordLyrical
{
    // Narrative seconds; CueTimeline remains the sole mapping from the master song.
    public static class BattleScore
    {
        public static float Ease(float u){u=Mathf.Clamp01(u);return u*u*u*(u*(u*6-15)+10);}
        public static float S(float a,float b,float t)=>Ease((t-a)/(b-a));
        public static float W(float a,float b,float c,float d,float t)=>S(a,b,t)*(1-S(c,d,t));
        public static float Duel(float t)=>W(67,69,100,104,t);
        public static float Shield(float t)=>W(68.5f,69,80.4f,81.3f,t);
        public static float Rescue(float t)=>W(74.8f,75.5f,79.8f,81,t);
        public static float Counter(float t)=>Mathf.Clamp01(W(71.5f,71.85f,72.6f,73.1f,t)+W(76,76.35f,80.4f,81,t)+W(85,85.3f,86.2f,86.65f,t)+W(90,90.25f,91.1f,91.6f,t)+W(97.5f,98,99.4f,100,t));
        public static float Recoil(float t)=>W(71.85f,72.1f,72.4f,73.4f,t)+W(85.3f,85.5f,85.85f,87,t)+W(90.25f,90.45f,90.8f,92,t);
        public static float Crouch(float t)=>.12f*Shield(t)+.14f*W(73.1f,73.5f,74,74.6f,t)+.10f*Rescue(t);
        public static Vector3 ShieldPoint(Vector3 jeru)=>jeru+new Vector3(-.18f,1.8f,.7f);
        public static float Support(float t)=>W(75,75.35f,80.4f,81,t)+W(92,92.5f,98.7f,99.5f,t);
        public static Vector3 SupportCenter(float t,Vector3 jeru,Vector3 heart)=>t<82?ShieldPoint(jeru):heart;
        public static float SupportRadius(float t)=>t<82?1.35f:1.7f;
    }
}
