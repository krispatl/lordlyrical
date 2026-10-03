using System;
using System.Collections.Generic;
namespace LordLyrical
{
    public sealed class CueScheduler
    {
        readonly Cue[] cues;int next;double previous=-1;
        public string CurrentId{get;private set;}="";
        public event Action<Cue> Fired;
        public CueScheduler(Cue[] source)
        {
            cues=source;var ids=new HashSet<string>();double t=-1;
            foreach(var c in cues){if(c.time<t||c.time<0||c.time>121||double.IsNaN(c.time)||!ids.Add(c.id))throw new ArgumentException("Invalid/duplicate cue: "+c.id);t=c.time;}
        }
        // Reconstruction evaluates persistent visuals directly. Only future transients are queued.
        public void Seek(double time){next=0;CurrentId="";while(next<cues.Length&&cues[next].time<=time){CurrentId=cues[next].id;next++;}previous=time;}
        public void Reset(){next=0;previous=-1;CurrentId="";}
        public void Advance(double time)
        {
            if(time<previous){Seek(time);return;}
            while(next<cues.Length&&cues[next].time<=time){var c=cues[next++];CurrentId=c.id;if(time-c.time<.25)Fired?.Invoke(c);}
            previous=time;
        }
    }
}
