using System;
using UnityEngine;
namespace LordLyrical
{
    [CreateAssetMenu(menuName="Lord Lyrical/Facial cue track")]
    public sealed class FacialCueTrack:ScriptableObject
    {
        [Serializable]public struct Channel{public string blendShape;public AnimationCurve weight;}
        public Channel[] channels=Array.Empty<Channel>();
    }
}
