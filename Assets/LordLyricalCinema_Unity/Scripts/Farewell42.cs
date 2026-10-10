using UnityEngine;
namespace LordLyrical {
 public static class Farewell42 {
  public static Vector3 Front(Vector3 eye,Quaternion gaze)=>eye+gaze*Vector3.forward*.85f+Vector3.down*.04f;
  public static float Influence(Vector3 fragment,Vector3 hand)=>1-CinemaTiming.S(.10f,.32f,Vector3.Distance(fragment,hand));
  public static HandPoseInput.HandState Closest(Vector3 p,HandPoseInput.HandState l,HandPoseInput.HandState r)=>!l.tracked?r:!r.tracked?l:Vector3.SqrMagnitude(l.position-p)<Vector3.SqrMagnitude(r.position-p)?l:r;
  public static Vector3 Respond(Vector3 p,HandPoseInput.HandState h,float influence)=>h.tracked?p+Vector3.ClampMagnitude(h.position+h.direction*.06f-p,.14f)*Mathf.Clamp01(influence)*.65f:p;
 }
}
