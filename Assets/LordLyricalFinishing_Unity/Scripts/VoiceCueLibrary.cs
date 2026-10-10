using System;
using UnityEngine;
namespace LordLyrical
{
    [CreateAssetMenu(menuName="Lord Lyrical/Voice cue library")]
    public sealed class VoiceCueLibrary:ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string cueId,actor;
            [TextArea]public string recordingDirection;
            [Tooltip("-1 is Jeru; 6 is the Seeker.")]public int slot=-1;
            public AudioClip recording;
            public FacialCueTrack facialPerformance;
            [Range(0,1)]public float gain=.65f;
        }
        public Entry[] entries=Array.Empty<Entry>();
    }
}
