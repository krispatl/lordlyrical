using UnityEngine;
namespace LordLyrical.Characters
{
    public sealed class HumanoidMotionLibrary : ScriptableObject
    {
        public AnimationClip idle,turnLeft,turnRight;
        // Clockwise from forward: forward, right-forward, right, right-back, back...
        public AnimationClip[] walk=new AnimationClip[8],run=new AnimationClip[8];
        public float walkStride=1.52f,runStride=2.28f;
        static HumanoidMotionLibrary cached;
        public static HumanoidMotionLibrary Load()=>cached?cached:(cached=Resources.Load<HumanoidMotionLibrary>("Concert/HumanoidMotions"));
    }
}
