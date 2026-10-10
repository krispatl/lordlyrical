using System;
using System.IO;
using System.Text;
using UnityEngine;
using LordLyrical;
public static class Production37Validation {
 static void Need(bool ok,string why){if(!ok)throw new Exception("PRODUCTION37: "+why);}
 public static void Run(){
  Production34Validation.OutputOverride=Production37Review.Out;try{Production34Validation.Run();}finally{Production34Validation.OutputOverride=null;}
  var log=new StringBuilder();
  Need(ShieldGeometry37.Intersect(new Vector3(0,0,2),Vector3.zero,Vector3.forward,Vector3.forward,.29f,out var point)&&Vector3.Distance(point,Vector3.forward)<.001f,"Shield plane hit");
  Need(!ShieldGeometry37.Intersect(new Vector3(1,0,2),new Vector3(1,0,0),Vector3.forward,Vector3.forward,.29f,out _),"Shield blocks outside radius");
  Need(!ShieldGeometry37.Intersect(Vector3.zero,new Vector3(0,0,2),Vector3.forward,Vector3.forward,.29f,out _),"Shield blocks from behind");
  var l=new HandPoseInput.HandState{tracked=true,closed=true,position=new Vector3(-.10f,1.2f,.55f)};var r=l;r.position=new Vector3(.10f,1.2f,.55f);
  Need(FistShield36.Join(false,l,r),"Merge fails");r.position=l.position+Vector3.right*.25f;Need(!FistShield36.Join(false,l,r)&&FistShield36.Join(true,l,r),"Hysteresis fails");r.tracked=false;Need(!FistShield36.Join(true,l,r),"Lost hand keeps merged shield");r=l;r.position=new Vector3(.10f,1.2f,.55f);
  float launch=DragonFireballScore.Launch(0);
  for(int mode=0;mode<4;mode++){
   var e=new ConcertEncounter();e.occupied[0]=true;e.heads[0]=new Vector3(0,1.65f,0);var s=e.State.dragonShots[0];s.armed=s.launched=true;s.slot=0;s.origin=new Vector3(0,1.2f,10);s.target=new Vector3(0,1.2f,0);
   int events=0;e.Impact+=(slot,blocked)=>events++;
   for(int f=0;f<145;f++){
    float t=launch+f/72f;var left=l;var right=r;
    if(mode==1){left.position+=Vector3.right*1.2f;right.position+=Vector3.right*1.2f;}
    if(mode==2&&t>launch+.7f){left.tracked=right.tracked=false;left.closed=right.closed=false;}
    if(mode!=3||f==0)e.SetShield(0,t,e.heads[0],left,right,true);
    e.guards[0]=t+.4f;e.Tick(t,1f/72);
   }
   Need(s.resolved&&events==1,"Contact repeats or fails to resolve");
   if(mode==0){Need(s.blocked&&s.blockerSlot==0&&s.impactTime<launch+DragonFireballScore.Flight&&Vector3.Distance(s.impactPoint,s.target)>.2f,"Fireball did not break at visible merged shield");Need(e.State.blocks==1&&e.State.hits==0,"Block statistics wrong");}
   else Need(!s.blocked&&e.State.hits==1,"Missed/lost/expired shield still blocks");
   e.Reset(1,0);Need(e.State.blocks==0&&!e.State.dragonShots[0].armed,"Replay retains combat");
  }
  Need(SharedFistShields37.Valid(new ShieldPacket37{slot=0,time=30,left=true,leftPosition=l.position},new Vector3(0,1.65f,0)),"Valid shield packet rejected");
  Need(!SharedFistShields37.Valid(new ShieldPacket37{slot=0,time=30,left=true,leftPosition=new Vector3(float.NaN,0,0)},Vector3.zero),"Nonfinite packet accepted");
  Need(!SharedFistShields37.Valid(new ShieldPacket37{slot=10,time=30},Vector3.zero),"Invalid slot accepted");
  Need(!SharedFistShields37.Valid(new ShieldPacket37{slot=0,time=30,left=true,leftPosition=Vector3.one*10},Vector3.zero),"Unreachable fist accepted");
  var combat=new ConcertEncounter();combat.occupied[0]=true;combat.heads[0]=new Vector3(0,1.65f,0);
  var enemy=combat.State.orcs[0];enemy.position=Vector3.forward*3;
  for(int i=1;i<12;i++)combat.State.orcs[i].position=new Vector3(20+i,0,10);
  combat.Pulse(0,30,.5f,0);float began=enemy.reactionAt;
  combat.Beam(0,30.1f,.5f,new Vector3(0,1.4f,0),Vector3.forward);
  Need(enemy.health<75&&Mathf.Abs(enemy.reactionAt-began)<.0001f&&ImpactReaction37.Weight(30.1f,enemy)>.9f,"Sustained beam restarts stagger impulse");
  Need(ImpactReaction37.Offset(30.1f,enemy).magnitude>.1f&&ImpactReaction37.Weight(30.6f,enemy)==0,"Orc impulse missing or never settles");
  string interaction=File.ReadAllText("Assets/LordLyricalConcert_Unity/Scripts/SharedFistShields37.cs");Need(interaction.Contains(".TryAccept(")&&interaction.Contains("OwnerOf(")&&interaction.Contains(".35f")&&interaction.Contains("InputEpochKey"),"Shield ownership/freshness gates missing");
  string cinema=File.ReadAllText("Assets/LordLyricalCinema_Unity/Scripts/CinemaPresentation.cs");Need(cinema.Contains("Survey.SetOcclusion(false,0)"),"Room mesh occlusion regressed");
  log.AppendLine("PASS front-face shield geometry, radius and rear-face rejection; merged hysteresis/tracking loss; authority fireball contact at shield surface; wrong-position, tracking-loss and expired-shield misses; exactly one damage/block event; replay reset; invalid/reachable packet data gates. Sustained beam damage preserves a distinct directional stagger impulse. Existing combat, audio-reactive and room regressions passed.");
  log.AppendLine("Shield transport uses assignment, run, input epoch, increasing sequence and 350ms freshness. Physical two-headset synchronization and native frame rate remain acceptance checks.");
  File.WriteAllText(Production37Review.Out+"/Combat37_Validation.txt",log.ToString());Debug.Log("PRODUCTION37_COMBAT_PASS\n"+log);
 }
}


