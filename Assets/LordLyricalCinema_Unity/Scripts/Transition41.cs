using UnityEngine;
namespace LordLyrical {
 public static class Transition41 {
  public static float Entrance(float songTime,bool waiting)=>waiting?0:songTime<9?CinemaTiming.S(8,9,songTime):1;
 }
}
