using UnityEngine;
namespace LordLyrical {
 public static class Presentation43Score {
  // Quiet only decorative layers; never dim warnings, hands or actual attacks.
  public static float Decorative(float t){float load=0;for(int i=0;i<DragonFireballScore.Count;i++)load=Mathf.Max(load,DragonFireballScore.Charge(t,i));return 1-.55f*load;}
  public static Vector3 DeathDirection(ConcertOrc o)=>Vector3.ProjectOnPlane(-o.lastHitNormal,Vector3.up).normalized;
  public static float DeathLean(float t,ConcertOrc o)=>o.health<=0?BattleScore.W(0,.20f,.45f,1.15f,t-o.defeatedAt):0;
  public static float Scar(float age)=>age>=0&&age<3.5f?CinemaTiming.S(0,.08f,age)*(1-CinemaTiming.S(.5f,3.5f,age)):0;
 }
}
