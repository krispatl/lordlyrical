using UnityEngine;
using UnityEngine.XR.Hands;
namespace LordLyrical {
 public sealed class HandTrackingQuality39 {
  public const float StaleSeconds=.10f,AcquireSeconds=.035f;
  float received=-100,started=-100;int samples,lastFrame=-1;bool valid;
  public void Clear(){received=started=-100;samples=0;lastFrame=-1;valid=false;}
  public bool Fresh(float now)=>valid&&now-received<=StaleSeconds&&now>=received;
  public bool Ready(float now)=>Fresh(now)&&samples>=2&&now-started>=AcquireSeconds;
  public void Observe(bool trustworthy,float now,int frame){
   if(!trustworthy){Clear();return;}
   if(!Fresh(now)){started=now;samples=0;lastFrame=-1;}
   valid=true;received=now;if(lastFrame!=frame){lastFrame=frame;samples++;}
  }
  public static bool Active(XRHandJointTrackingState flags)=>(flags&(XRHandJointTrackingState.Pose|XRHandJointTrackingState.HighFidelityPose))==(XRHandJointTrackingState.Pose|XRHandJointTrackingState.HighFidelityPose);
  public static bool Finite(Pose p){float n=Quaternion.Dot(p.rotation,p.rotation);return ManualResonanceBeams.Finite(p.position)&&float.IsFinite(n)&&n>.9f&&n<1.1f;}
 }
}
