using UnityEngine;
namespace LordLyrical {
 public static class ImpactReaction37 {
  public static float Weight(float t,ConcertOrc orc)=>orc.health>0&&orc.intent==OrcIntent.Stagger?BattleScore.W(0,.06f,.14f,.50f,t-orc.reactionAt):0;
  public static Vector3 Offset(float t,ConcertOrc orc)=>Vector3.ProjectOnPlane(-orc.reactionNormal,Vector3.up).normalized*Weight(t,orc)*.13f;
 }
}
