using UnityEngine;
namespace LordLyrical {
 public static class ShieldGeometry37 {
  public const float SingleRadius=.29f,MergedRadius=.68f;
  public static Vector3 Normal(Vector3 head,Vector3 fist){var n=fist-head;return n.sqrMagnitude>.001f?n.normalized:Vector3.forward;}
  public static Vector3 Centre(Vector3 head,Vector3 fist,bool merged)=>fist+Normal(head,fist)*(merged?.23f:.13f);
  public static bool Intersect(Vector3 a,Vector3 b,Vector3 centre,Vector3 normal,float radius,out Vector3 point){
   point=default;float from=Vector3.Dot(a-centre,normal),to=Vector3.Dot(b-centre,normal);
   // Incoming projectiles approach the outward face; opening behind the shield cannot block.
   if(from<0||to>0||from-to<.00001f)return false;
   point=Vector3.Lerp(a,b,from/(from-to));return Vector3.ProjectOnPlane(point-centre,normal).sqrMagnitude<=(radius+.16f)*(radius+.16f);
  }
 }
}
