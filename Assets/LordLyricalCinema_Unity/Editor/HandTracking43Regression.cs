using System;
using System.IO;
using UnityEngine;
using UnityEngine.XR.Hands;
using LordLyrical;
public static class HandTracking43Regression {
 static void Need(bool ok,string why){if(!ok)throw new Exception("HAND39: "+why);}
 public static void Run(){
  var q=new HandTrackingQuality39();Need(!q.Ready(0),"initial ghost");q.Observe(true,0,0);Need(!q.Ready(0),"single-frame acquisition");q.Observe(true,.02f,1);Need(!q.Ready(.02f),"acquisition stability missing");q.Observe(true,.04f,2);Need(q.Ready(.04f),"stable tracking rejected");q.Observe(false,.05f,3);Need(!q.Ready(.05f),"inferred/lost frame frozen");q.Observe(true,.06f,4);Need(!q.Ready(.06f),"reacquisition uses old samples");q.Observe(true,.1f,5);Need(q.Ready(.1f)&&!q.Ready(.201f),"stalled-provider timeout");q.Clear();Need(!q.Ready(.11f),"focus/reset retains tracking");
  Need(!HandTrackingQuality39.Active(XRHandJointTrackingState.Pose)&&HandTrackingQuality39.Active(XRHandJointTrackingState.Pose|XRHandJointTrackingState.HighFidelityPose),"valid inference treated as active tracking");
  Need(!HandTrackingQuality39.Finite(new Pose(new Vector3(float.NaN,0,0),Quaternion.identity))&&!HandTrackingQuality39.Finite(new Pose(Vector3.zero,new Quaternion(0,0,0,0))),"invalid native pose accepted");
  Need(HandPresence39.Valid(new HandPresencePacket39{slot=0,sequence=1,tracked=3,open=1}),"valid presence rejected");Need(!HandPresence39.Valid(new HandPresencePacket39{slot=10,sequence=1})&&!HandPresence39.Valid(new HandPresencePacket39{slot=0,sequence=1,tracked=0,open=1}),"invalid presence accepted");Need(HandPresence39.Fresh(1.29f,1)&&!HandPresence39.Fresh(1.31f,1),"remote heartbeat does not expire");
  var prefab=Resources.Load<GameObject>("LordLyrical/TrackedLeftHand");var go=UnityEngine.Object.Instantiate(prefab);try{var hand=go.GetComponent<TrackedHandMesh>();var poses=new Pose[26];for(int i=0;i<26;i++)poses[i]=new Pose(hand.joints[i].position,hand.joints[i].rotation);hand.ApplyJointPoses(poses,true);Need(hand.Visible&&hand.JointPositionError<.00001f,"accurate valid joints rejected");hand.Hide();Need(!hand.Visible,"mesh sticks after loss");hand.ApplyJointPoses(poses,false);Need(!hand.Visible,"old pose resurrects invisible hand");}finally{UnityEngine.Object.DestroyImmediate(go);}
  var input=File.ReadAllText("Assets/LordLyrical/Scripts/XR/HandPoseInput.cs");Need(input.Contains("subsystem.updatedHands+=OnHandsUpdated")&&input.Contains("subsystem.trackingLost+=OnTrackingLost")&&input.Contains("UpdateType.BeforeRender")&&input.Contains("Ready(Time.unscaledTime)"),"quality callbacks not wired");
  Directory.CreateDirectory(Production43Review.Out);File.WriteAllText(Production43Review.Out+"/HandTracking_Validation.txt","PASS active-vs-inferred root confidence; immediate loss; 100ms stalled-provider timeout; stable reacquisition; focus/reset; finite joint poses; exact mesh joint application; mesh hide; 300ms remote heartbeat expiry and packet masks. Requires a worn-PICO check that native palm/wrist HighFidelityPose bits are supplied and that leaving/returning to view behaves correctly.\n");Debug.Log("HAND39_PASS");
 }
}
