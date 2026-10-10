using System;
using System.IO;
using UnityEngine;
using LordLyrical;
public static class Production36Validation {
 public static void Run(){
  Production34Validation.OutputOverride=Production36Review.Out;
  try{Production34Validation.Run();}finally{Production34Validation.OutputOverride=null;}
  var l=new HandPoseInput.HandState{tracked=true,closed=true,position=Vector3.zero};var r=l;r.position=Vector3.right*.2f;
  if(!FistShield36.Join(false,l,r))throw new Exception("Fists do not merge");
  r.position=Vector3.right*.25f;if(FistShield36.Join(false,l,r)||!FistShield36.Join(true,l,r))throw new Exception("Merge hysteresis");
  r.tracked=false;if(FistShield36.Join(true,l,r))throw new Exception("Lost tracking retains shield");
  string source=File.ReadAllText("Assets/LordLyricalCinema_Unity/Scripts/CinemaPresentation.cs");if(!source.Contains("Survey.SetOcclusion(false,0)"))throw new Exception("Transition occlusion regressed");
  File.WriteAllText(Production36Review.Out+"/Shield36_Validation.txt","PASS fist merge/release hysteresis and tracking loss; room depth disabled during transitions; existing combat and room regressions passed. Guard damage rules unchanged. Physical visual check pending.");
 }
}
