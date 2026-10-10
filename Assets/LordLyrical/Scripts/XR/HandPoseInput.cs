using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace LordLyrical
{
    // Hand poses and the headset share the physically aligned XR origin.
    public sealed class HandPoseInput : MonoBehaviour
    {
        public struct HandState { public bool tracked,open,closed;public int validFingers;public Vector3 position,direction;public Quaternion rotation; }
        public HandState Left {get;private set;}
        public HandState Right {get;private set;}
        public bool Available=>subsystem!=null&&subsystem.running;
        public bool LeftNativeTracked=>Available&&subsystem.leftHand.isTracked;
        public bool RightNativeTracked=>Available&&subsystem.rightHand.isTracked;
        public int SupportedJointCount {get{if(subsystem==null)return 0;int count=0;var layout=subsystem.jointsInLayout;for(int i=0;i<layout.Length;i++)if(layout[i])count++;return count;}}
        public int NativeUpdateFlags=>Available?(int)subsystem.updateSuccessFlags:0;
        public bool NativeExtensionEnabled=>Available&&UnityEngine.XR.OpenXR.OpenXRRuntime.IsExtensionEnabled(UnityEngine.XR.Hands.OpenXR.HandTracking.extensionString);
        public const string HandPermission="com.picovr.permission.HAND_TRACKING";
#if UNITY_ANDROID && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void DeferHandProviderUntilPermission()
        {
            // OpenXRHandProvider initializes its native tracker once, when its
            // joint layout is created. Do not do that before the OS grants access.
            UnityEngine.XR.Hands.OpenXR.HandTracking.automaticallyInitializeSubsystem=false;
        }
        void EnsureAuthorizedHandProvider()
        {
            if(UnityEngine.XR.Hands.OpenXR.HandTracking.subsystem!=null)return;
            var settings=UnityEngine.XR.Management.XRGeneralSettings.Instance;
            var manager=settings?settings.Manager:null;
            if(manager==null||!manager.isInitializationComplete||manager.activeLoader==null)return;
            var xr=UnityEngine.XR.OpenXR.OpenXRSettings.Instance;
            var feature=xr?xr.GetFeature<UnityEngine.XR.Hands.OpenXR.HandTracking>():null;
            if(feature==null||!feature.enabled)return;
            UnityEngine.XR.Hands.OpenXR.HandTracking.EnsureSubsystemInitialized();
            Debug.Log("LL_HAND_ACCESS initialized provider after permission");
        }
        bool permissionGranted,permissionRequestPending,permissionAttempted;
        float nextPermissionCheck;
        UnityEngine.Android.PermissionCallbacks permissionCallbacks;
        public bool PermissionGranted=>permissionGranted;
        public bool PermissionRequestPending=>permissionRequestPending;
        void Update()
        {
            if(!Application.isFocused||Time.unscaledTime<nextPermissionCheck)return;
            nextPermissionCheck=Time.unscaledTime+.5f;
            permissionGranted=UnityEngine.Android.Permission.HasUserAuthorizedPermission(HandPermission);
            if(permissionGranted){permissionRequestPending=false;EnsureAuthorizedHandProvider();return;}
            if(permissionAttempted)return;
            permissionAttempted=true;permissionRequestPending=true;
            permissionCallbacks=new UnityEngine.Android.PermissionCallbacks();
            permissionCallbacks.PermissionGranted+=PermissionResult;
            permissionCallbacks.PermissionDenied+=PermissionResult;
            Debug.Log("LL_HAND_ACCESS requesting hand-tracking permission");
            UnityEngine.Android.Permission.RequestUserPermission(HandPermission,permissionCallbacks);
        }
        void PermissionResult(string name)
        {
            if(!this||name!=HandPermission)return;
            permissionGranted=UnityEngine.Android.Permission.HasUserAuthorizedPermission(HandPermission);
            permissionRequestPending=false;nextPermissionCheck=0;
            Debug.Log("LL_HAND_ACCESS granted="+permissionGranted);
        }
        void OnApplicationFocus(bool focused)
        {
            nextPermissionCheck=0;
            if(!focused)ClearTracking();
        }
#else
        public bool PermissionGranted=>true;
        public bool PermissionRequestPending=>false;
#endif
        readonly List<XRHandSubsystem> systems=new();
        XRHandSubsystem subsystem;
        readonly HandTrackingQuality39 leftQuality=new(),rightQuality=new();
        float nextQualityLog;
        public int LeftPalmFlags {get;private set;}public int RightPalmFlags {get;private set;}
        void ClearTracking(){leftQuality.Clear();rightQuality.Clear();Left=default;Right=default;if(rig){if(rig.LeftVisual)rig.LeftVisual.Hide();if(rig.RightVisual)rig.RightVisual.Hide();}}
        void Bind(XRHandSubsystem next){if(subsystem==next)return;if(subsystem!=null){subsystem.updatedHands-=OnHandsUpdated;subsystem.trackingLost-=OnTrackingLost;}ClearTracking();subsystem=next;if(subsystem!=null){subsystem.updatedHands+=OnHandsUpdated;subsystem.trackingLost+=OnTrackingLost;}}
        void OnDisable(){Bind(null);ClearTracking();}
        void OnApplicationPause(bool paused){if(paused)ClearTracking();}
        void OnTrackingLost(XRHand hand){if(hand.handedness==Handedness.Left){leftQuality.Clear();Left=default;if(rig&&rig.LeftVisual)rig.LeftVisual.Hide();}else{rightQuality.Clear();Right=default;if(rig&&rig.RightVisual)rig.RightVisual.Hide();}}
        void OnHandsUpdated(XRHandSubsystem source,XRHandSubsystem.UpdateSuccessFlags flags,XRHandSubsystem.UpdateType type){
            if(!rig||!Application.isFocused||!PermissionGranted){ClearTracking();return;}
            Observe(true,flags);Observe(false,flags);Left=Read(true);Right=Read(false);
            if(Time.unscaledTime>=nextQualityLog){nextQualityLog=Time.unscaledTime+5;Debug.Log($"LL_HAND39 active={Left.tracked}/{Right.tracked} native={LeftNativeTracked}/{RightNativeTracked} palmFlags={LeftPalmFlags}/{RightPalmFlags} updateFlags={(int)flags}");}
            if(type==XRHandSubsystem.UpdateType.BeforeRender){if(rig.LeftVisual)rig.LeftVisual.UpdatePose(this);if(rig.RightVisual)rig.RightVisual.UpdatePose(this);}
        }
        void Observe(bool left,XRHandSubsystem.UpdateSuccessFlags flags){
            var h=left?subsystem.leftHand:subsystem.rightHand;var q=left?leftQuality:rightQuality;var palm=h.GetJoint(XRHandJointID.Palm);var wrist=h.GetJoint(XRHandJointID.Wrist);
            var required=left?XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose|XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints:XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose|XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
            if(left)LeftPalmFlags=(int)palm.trackingState;else RightPalmFlags=(int)palm.trackingState;
            bool good=(flags&required)==required&&h.isTracked&&HandTrackingQuality39.Active(palm.trackingState)&&HandTrackingQuality39.Active(wrist.trackingState)&&palm.TryGetPose(out var p)&&wrist.TryGetPose(out var w)&&HandTrackingQuality39.Finite(p)&&HandTrackingQuality39.Finite(w);
            q.Observe(good,Time.unscaledTime,Time.frameCount);
        }
        TrackedRig rig;
        static readonly XRHandJointID[] bases={XRHandJointID.IndexProximal,XRHandJointID.MiddleProximal,XRHandJointID.RingProximal,XRHandJointID.LittleProximal};
        static readonly XRHandJointID[] mids={XRHandJointID.IndexIntermediate,XRHandJointID.MiddleIntermediate,XRHandJointID.RingIntermediate,XRHandJointID.LittleIntermediate};
        static readonly XRHandJointID[] ends={XRHandJointID.IndexDistal,XRHandJointID.MiddleDistal,XRHandJointID.RingDistal,XRHandJointID.LittleDistal};
        static readonly XRHandJointID[] tips={XRHandJointID.IndexTip,XRHandJointID.MiddleTip,XRHandJointID.RingTip,XRHandJointID.LittleTip};
        public static float Extension(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {float length=Vector3.Distance(a,b)+Vector3.Distance(b,c)+Vector3.Distance(c,d);return length>.015f?Vector3.Distance(a,d)/length:0;}
        public struct GestureEvidence
        {
            public int ValidFingers {get;private set;}
            int straight,folded;
            public bool Open=>straight>=3;
            public bool Closed=>folded>=3;
            public void AddFinger(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                if(!ManualResonanceBeams.Finite(a)||!ManualResonanceBeams.Finite(b)||!ManualResonanceBeams.Finite(c)||!ManualResonanceBeams.Finite(d))return;
                float length=Vector3.Distance(a,b)+Vector3.Distance(b,c)+Vector3.Distance(c,d);
                if(length<=.015f)return;
                ValidFingers++;float extension=Vector3.Distance(a,d)/length;
                if(extension>.87f)straight++;if(extension<.65f)folded++;
            }
        }
        public void Refresh(TrackedRig owner)
        {
            rig=owner;
            if(subsystem==null||!subsystem.running){systems.Clear();SubsystemManager.GetSubsystems(systems);XRHandSubsystem next=null;foreach(var item in systems)if(item.running){next=item;break;}Bind(next);}
            Left=Read(true);Right=Read(false);
        }
        public bool TryJointPose(bool left,XRHandJointID id,out Pose worldPose)
        {
            worldPose=default;if(!Available||!(left?Left.tracked:Right.tracked))return false;
            var h=left?subsystem.leftHand:subsystem.rightHand;
            if(!h.GetJoint(id).TryGetPose(out var local)||!HandTrackingQuality39.Finite(local))return false;
            worldPose=new Pose(rig.transform.TransformPoint(local.position),rig.transform.rotation*local.rotation);return true;
        }
        HandState Read(bool left)
        {
            if(!Available||!PermissionGranted||!Application.isFocused||!(left?leftQuality:rightQuality).Ready(Time.unscaledTime))return default;
            var h=left?subsystem.leftHand:subsystem.rightHand;
            if(!h.isTracked||!h.GetJoint(XRHandJointID.Palm).TryGetPose(out var palm)||!h.GetJoint(XRHandJointID.Wrist).TryGetPose(out var wrist))return default;
            var evidence=new GestureEvidence();
            for(int i=0;i<4;i++){
                if(!HandTrackingQuality39.Active(h.GetJoint(bases[i]).trackingState)||!HandTrackingQuality39.Active(h.GetJoint(tips[i]).trackingState))continue;
                if(!h.GetJoint(bases[i]).TryGetPose(out var a)||!h.GetJoint(mids[i]).TryGetPose(out var b)||!h.GetJoint(ends[i]).TryGetPose(out var c)||!h.GetJoint(tips[i]).TryGetPose(out var d))continue;
                evidence.AddFinger(a.position,b.position,c.position,d.position);
            }
            if(!HandTrackingQuality39.Finite(palm)||!HandTrackingQuality39.Finite(wrist)||Vector3.Distance(palm.position,wrist.position)>.25f)return default;
            var origin=rig.transform;
            var result=new HandState{tracked=true,validFingers=evidence.ValidFingers,position=origin.TransformPoint(palm.position),rotation=origin.rotation*wrist.rotation};
            // XR Hands defines palm outward as local -Y, for both hands.
            result.direction=origin.TransformDirection(palm.rotation*Vector3.down).normalized;
            if(!rig.head||Vector3.Distance(result.position,rig.head.position)>1.8f)return default;
            Vector3 away=result.position-rig.head.position;
            result.open=evidence.Open&&away.magnitude>.22f&&Vector3.Dot(result.direction,away.normalized)>.35f&&result.position.y>rig.head.position.y-.7f;
            result.closed=evidence.Closed;
            var target=left?rig.left:rig.right;
            if(target){target.localPosition=wrist.position;target.localRotation=wrist.rotation;}
            return result;
        }
    }
}
