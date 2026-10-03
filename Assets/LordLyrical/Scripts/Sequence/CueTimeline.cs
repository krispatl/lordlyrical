using System;
namespace LordLyrical
{
    // Piecewise time mapping lets artists retime cues while preserving the authored poses
    // and continuous transitions between them. Music/DSP time itself is never warped.
    public sealed class CueTimeline
    {
        readonly Cue[] markers;
        public CueTimeline(Cue[] cues)
        {
            markers=cues;if(cues.Length<2)throw new ArgumentException("Cue timeline requires endpoints");
            for(int i=1;i<cues.Length;i++)if(cues[i].time<=cues[i-1].time||cues[i].authoredTime<=cues[i-1].authoredTime)throw new ArgumentException("Retimed cues must preserve order and distinct times");
            if(cues[0].time!=0||cues[^1].time!=121)throw new ArgumentException("Keep the 0 and 121 second show endpoints");
        }
        public double NarrativeTime(double musicTime)
        {
            if(musicTime<=markers[0].time)return markers[0].authoredTime;
            for(int i=1;i<markers.Length;i++)if(musicTime<=markers[i].time){var a=markers[i-1];var b=markers[i];double fraction=(musicTime-a.time)/(b.time-a.time);return a.authoredTime+(b.authoredTime-a.authoredTime)*fraction;}
            return markers[^1].authoredTime;
        }
    }
}
