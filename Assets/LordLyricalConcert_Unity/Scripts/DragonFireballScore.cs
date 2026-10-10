using System;
using UnityEngine;
namespace LordLyrical
{
    [Serializable] public sealed class DragonShot
    {
        public bool armed,launched,resolved,blocked; public int slot=-1;
        public Vector3 origin,target,impactPoint,impactNormal;public float impactTime=-100;public int blockerSlot=-1;
    }
    // Authority locks one target during the tell. All clients evaluate the same flight.
    public static class DragonFireballScore
    {
        public const int Count=3;
        public const float Windup=.95f,Flight=1.45f,Afterglow=.65f;
        public static float Launch(int index)=>82.8f+index*3.15f;
        public static float Charge(float t,int index)=>BattleScore.W(Launch(index)-Windup,Launch(index)-.10f,Launch(index),Launch(index)+.12f,t);
        public static float Aim(float t,int index)=>BattleScore.W(Launch(index)-Windup,Launch(index)-.25f,Launch(index)+.12f,Launch(index)+.55f,t);
        public static Vector3 Position(DragonShot shot,float age)
        {
            float p=Mathf.Clamp01(age/Flight);
            return Vector3.Lerp(shot.origin,shot.target,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*.48f);
        }
        public static DragonShot[] Empty(){var s=new DragonShot[Count];for(int i=0;i<s.Length;i++)s[i]=new DragonShot();return s;}
    }
}
