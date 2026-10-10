using UnityEngine;
using UnityEngine.XR.Hands;

namespace LordLyrical
{
    // A dedicated XR skeleton. Never inherits humanoid body scale or animation.
    public sealed class TrackedHandMesh : MonoBehaviour
    {
        public bool left;
        public SkinnedMeshRenderer skin;
        public XRHandJointID[] jointIds;
        public Transform[] joints;
        readonly Pose[] sample = new Pose[26];
        public bool Visible => skin && skin.enabled;
        public float JointPositionError { get; private set; }
        public int AppliedJointCount { get; private set; }

        MagicalHandPresentation magic;
        void Awake() { Hide(); }
        void OnDisable() { Hide(); }
        public void Hide()
        {
            if (skin) skin.enabled = false;
            if(magic)magic.Clear();
            AppliedJointCount = 0;
            JointPositionError = 0;
        }
        static bool Finite(Pose pose)
        {
            var q = pose.rotation;
            float length = q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w;
            return ManualResonanceBeams.Finite(pose.position) &&
                float.IsFinite(length) && length > .9f && length < 1.1f;
        }
        public void UpdatePose(HandPoseInput input)
        {
            if (!input || !(left ? input.Left.tracked : input.Right.tracked) ||
                jointIds == null || jointIds.Length != sample.Length) { Hide(); return; }
            // Read the complete frame before changing any joint. A partial frame must
            // never combine a new wrist with yesterday's finger positions.
            for (int i = 0; i < sample.Length; i++)
                if (!input.TryJointPose(left, jointIds[i], out sample[i])) { Hide(); return; }
            ApplyJointPoses(sample, true);
            if(Visible){
                if(!magic){magic=gameObject.AddComponent<MagicalHandPresentation>();magic.Initialize(this);}
                magic.Render(Magic33Score.Clock,left?input.Left:input.Right,new Color(.24f,.68f,1));
            }
        }
        public void ApplyJointPoses(Pose[] poses, bool tracked)
        {
            if (!tracked || poses == null || poses.Length != 26 || joints == null ||
                joints.Length != 26 || !skin) { Hide(); return; }
            for (int i = 0; i < poses.Length; i++)
                if (!joints[i] || !Finite(poses[i]) ||
                    Vector3.Distance(poses[0].position, poses[i].position) > .45f) { Hide(); return; }
            // References are serialized wrist first, then each parent before its
            // children. XR Hands and these sample meshes use the same joint axes.
            for (int i = 0; i < poses.Length; i++)
                joints[i].SetPositionAndRotation(poses[i].position, poses[i].rotation);
            JointPositionError = 0;
            for (int i = 0; i < poses.Length; i++)
                JointPositionError = Mathf.Max(JointPositionError,
                    Vector3.Distance(joints[i].position, poses[i].position));
            AppliedJointCount = poses.Length;
            skin.enabled = true;
        }
    }
}
