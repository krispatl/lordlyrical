using UnityEngine;
namespace LordLyrical {
 public static class StoryScore40 {
  public static float Awakening(float t)=>BattleScore.W(21,21.6f,24.8f,26,t);
  public static float Reveal(float t)=>BattleScore.W(63,63.3f,65.8f,69,t);
  public static float Discord(float t)=>BattleScore.W(69,69.3f,79.7f,81,t);
  public static float Quiet(float t)=>1-.78f*BattleScore.W(96,96.4f,97.8f,98,t);
  public static float Art(float t)=>BattleScore.W(98,101,110.4f,111,t);
  public static float Farewell(float t)=>CinemaTiming.S(119.2f,120,t)*(1-CinemaTiming.S(121.5f,124,t));
  public static float Trace(float t)=>t<9?BattleScore.W(-.8f,-.45f,1.6f,4,t)*.22f:BattleScore.W(118.2f,119,121.5f,124,t)*.22f;
 }
}
