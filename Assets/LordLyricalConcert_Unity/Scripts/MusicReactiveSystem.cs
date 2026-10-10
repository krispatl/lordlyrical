using System;
using UnityEngine;

namespace LordLyrical
{
    [Serializable] public sealed class MusicScore
    {
        public float sampleRate,duration,estimatedBpm;
        public float[] amplitude,bass,mid,air,beats,strengths;
    }
    public readonly struct MusicFrame
    {
        public readonly float time,amplitude,bass,mid,air,pulse,beatDistance;
        public readonly int beat,section;
        public MusicFrame(float t,float a,float b,float m,float h,float p,float d,int i,int s)
        {time=t;amplitude=a;bass=b;mid=m;air=h;pulse=p;beatDistance=d;beat=i;section=s;}
    }
    // Shared offline analysis avoids divergent FFT sampling across network clients.
    public sealed class MusicReactiveSystem
    {
        public static MusicFrame Current {get;private set;}
        public static MusicScore Score {get;private set;}
        public event Action<MusicFrame> OnBeat,OnStrongBeat,OnSectionChanged,OnVocalPeak,OnDrop;
        float previous=-1;int lastBeat=-1,lastSection=-1;
        public MusicReactiveSystem(){if(Score==null){var asset=Resources.Load<TextAsset>("Concert/MusicScore");if(asset)Score=JsonUtility.FromJson<MusicScore>(asset.text);}}
        static float Value(float[] a,float time){if(a==null||a.Length==0)return 0;float f=Mathf.Clamp(time*Score.sampleRate,0,a.Length-1);int i=(int)f;return Mathf.Lerp(a[i],a[Math.Min(i+1,a.Length-1)],f-i);}
        public static MusicFrame Sample(float time)
        {
            if(Score==null||Score.beats==null)return default;
            int i=Array.BinarySearch(Score.beats,time);if(i<0)i=~i-1;
            float age=i>=0?time-Score.beats[i]:100,dist=age;
            if(i+1<Score.beats.Length)dist=Mathf.Min(dist,Score.beats[i+1]-time);
            int section=time<21?0:time<26?1:time<54?2:time<63?3:time<81?4:time<92?5:time<101?6:time<111?7:8;
            return new MusicFrame(time,Value(Score.amplitude,time),Value(Score.bass,time),Value(Score.mid,time),Value(Score.air,time),Mathf.Exp(-age*8)*(i>=0?Mathf.Lerp(.35f,1,Score.strengths[i]):0),dist,i,section);
        }
        public void Evaluate(float time,bool live)
        {
            Current=Sample(time);bool continuous=previous>=0&&time>=previous&&time-previous<.35f;
            if(live&&continuous){
                if(Current.beat!=lastBeat){OnBeat?.Invoke(Current);if(Current.pulse>.6f)OnStrongBeat?.Invoke(Current);}
                if(Current.section!=lastSection){OnSectionChanged?.Invoke(Current);if(Current.section==4||Current.section==6)OnDrop?.Invoke(Current);}
            }
            lastBeat=Current.beat;lastSection=Current.section;previous=time;
        }
        public void VocalPeak(){OnVocalPeak?.Invoke(Current);}
        public void Reset(){previous=-1;lastBeat=-1;lastSection=-1;Current=default;}
    }
}
