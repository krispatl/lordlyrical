using System;
using UnityEngine;
namespace LordLyrical
{
    public class AudioMasterClock:MonoBehaviour
    {
        public AudioSource source;
        // Leave headroom for decoded MP3 peaks and the spatial accent bus.
        public const float MasterGain=.9f;
        public double TimeSeconds{get;private set;}public double DriftSeconds{get;private set;}
        public bool Scheduled{get;private set;}
        double dspStart,seekTime,lastCheck;bool finished;
        public void Stop(){if(source!=null){source.Stop();source.pitch=1;source.volume=MasterGain;}TimeSeconds=0;Scheduled=false;finished=false;}
        public void Schedule(double showTime)
        {
            Stop();if(source==null||source.clip==null)return;
            double lead=showTime<0?-showTime:.15;
            seekTime=showTime<0?0:Math.Min(121,showTime+lead);
            if(seekTime>=121){TimeSeconds=121;finished=true;return;}
            source.timeSamples=Math.Clamp((int)(seekTime*source.clip.frequency),0,source.clip.samples-1);
            dspStart=AudioSettings.dspTime+lead;source.PlayScheduled(dspStart);Scheduled=true;lastCheck=dspStart;
        }
        public void Tick(double target)
        {
            if(!Scheduled||finished)return;
            double dsp=AudioSettings.dspTime;
            if(dsp<dspStart){TimeSeconds=target;return;}
            // DSP scheduled anchor has sub-frame precision. Sample position reconciles drift.
            TimeSeconds=seekTime+(dsp-dspStart);
            if(dsp-lastCheck>.25&&source.isPlaying)
            {
                double sampleTime=source.timeSamples/(double)source.clip.frequency;
                DriftSeconds=sampleTime-target;
                if(Math.Abs(DriftSeconds)>.18){Schedule(target);return;}
                source.pitch=1+(float)Math.Clamp(-DriftSeconds*.10,-.003,.003);
                // Avoid sample-buffer stepping in visual time while following accumulated pitch correction.
                seekTime=sampleTime;dspStart=dsp;lastCheck=dsp;
            }
            source.volume=MasterGain*(target>120?(float)Math.Clamp(121-target,0,1):1);
            if(target>=121){source.Stop();TimeSeconds=121;finished=true;}
        }
    }
}
