using System;
using System.IO;
using System.Linq;
using UnityEngine;
using LordLyrical;
public static class Production43CombatRegression {
 static void Need(bool ok,string why){if(!ok)throw new Exception("PRODUCTION38: "+why);}
 static ConcertEncounter Combat(){var e=new ConcertEncounter();e.occupied[0]=true;e.heads[0]=new Vector3(0,1.65f,0);e.State.orcs[0].position=Vector3.forward*3;for(int i=1;i<12;i++)e.State.orcs[i].position=new Vector3(30+i,0,10);return e;}
 public static void Run(){
  var c=new CounterCharge38();Counterattack38.Grant(c,83);Need(c.expires==91,"eight-second expiry");Counterattack38.Grant(c,84);Need(c.expires==91,"charge stacking/refresh");
  Need(Counterattack38.Beam(c,84,0)==2&&c.releaseSerial==1&&c.expires<0,"single release");Need(Counterattack38.Beam(c,84.1f,1)==1&&c.releaseSerial==1,"both hands enhanced");Need(Counterattack38.Beam(c,84.2f,0)==2&&c.releaseSerial==1,"release duplicated");Need(Counterattack38.Beam(c,85,0)==1,"boost exceeds one second");
  Counterattack38.Grant(c,86);Need(Counterattack38.Beam(c,94,0)==1&&c.releaseSerial==1,"expired charge releases");Counterattack38.Grant(c,95);Counterattack38.Cancel(c);Need(Counterattack38.Beam(c,95.1f,0)==1,"cancel retains charge");
  var normal=Combat();var boosted=Combat();Counterattack38.Grant(boosted.State.counters[0],29);
  normal.Beam(0,30,.5f,new Vector3(0,1.4f,0),Vector3.forward);boosted.Beam(0,30,.5f,new Vector3(0,1.4f,0),Vector3.forward);
  Need(Mathf.Abs((100-normal.State.orcs[0].health)*2-(100-boosted.State.orcs[0].health))<.001f,"damage multiplier");
  var json=JsonUtility.ToJson(boosted.State);var replica=JsonUtility.FromJson<ConcertState>(json);Need(Counterattack38.Valid(replica.counters)&&replica.counters[0].releaseSerial==1&&Counterattack38.Enhanced(replica.counters[0],30.5f,0),"remote release snapshot");
  boosted.Accept(replica);Need(Counterattack38.Beam(boosted.State.counters[0],30.6f,0)==2&&boosted.State.counters[0].releaseSerial==1,"duplicate snapshot replays release");
  var l=new HandPoseInput.HandState{tracked=true,closed=true,position=new Vector3(0,1.2f,.6f)};var e=Combat();Counterattack38.Grant(e.State.counters[0],30);e.SetShield(0,30,e.heads[0],default,default,false);Need(e.State.counters[0].expires<0,"tracking loss retains charge");
  Counterattack38.Grant(e.State.counters[0],31);e.SetShield(0,31,e.heads[0],l,default,false);e.Tick(31.2f,.05f);Need(e.State.counters[0].expires<0,"stale pose retains charge");Counterattack38.Grant(e.State.counters[0],32);e.occupied[0]=false;e.Tick(32,.01f);Need(e.State.counters[0].expires<0,"disconnect retains charge");e.Reset(1,0);Need(e.State.counters.All(x=>x.releaseSerial==0&&x.expires<0),"reset retains release");
  // A real shield interception grants energy once; releasing its fist keeps tracking live.
  e=new ConcertEncounter();e.occupied[0]=true;e.heads[0]=new Vector3(0,1.65f,0);var shot=e.State.dragonShots[0];shot.armed=shot.launched=true;shot.slot=0;shot.origin=new Vector3(0,1.2f,10);shot.target=new Vector3(0,1.2f,0);
  for(int f=0;f<110;f++){float t=DragonFireballScore.Launch(0)+f/72f;e.SetShield(0,t,e.heads[0],l,l,true);e.Tick(t,1f/72);}
  Need(shot.blocked&&e.State.counters[0].expires>shot.impactTime,"actual block does not grant charge");float expiry=e.State.counters[0].expires;e.Tick(84.35f,.01f);Need(e.State.counters[0].expires==expiry,"repeat grant");
  for(int people=0;people<=10;people+=people==0?1:9){
   var group=new ConcertEncounter();group.State.dragonHeart=new Vector3(0,8,11);for(int slot=0;slot<people;slot++){group.occupied[slot]=true;group.heads[slot]=new Vector3(slot*.1f,1.65f,0);}
   for(int f=0;f<60;f++){float t=92+f*.1f;for(int slot=0;slot<people;slot++){Vector3 origin=group.heads[slot];group.Beam(slot,t,1,origin,(group.State.dragonHeart-origin).normalized);}}
   Need(people==0?group.State.finaleCharge==0:group.State.finaleCharge>.9f,"cooperation normalization "+people);float before=group.State.finaleCharge;group.Beam(0,98.1f,1,Vector3.zero,group.State.dragonHeart.normalized);Need(group.State.finaleCharge==before,"charge after finishing cue");
   Need(Crescendo38.Envelope(98,group.State.finaleCharge)==1&&Crescendo38.Envelope(101,group.State.finaleCharge)==0,"fixed finale timing");
  }
  Need(Crescendo38.Envelope(91,1)==0&&Crescendo38.Envelope(95,0)==0,"premature crescendo");
  string cinema=File.ReadAllText("Assets/LordLyricalCinema_Unity/Scripts/CinemaPresentation.cs"),room=File.ReadAllText("Assets/LordLyricalCinema_Unity/Resources/Cinema26/RoomResonance.shader");
  Need(cinema.Contains("Survey.SetOcclusion(false,0)")&&cinema.Contains("bool measured=Survey.TrySurfacePoint")&&cinema.Contains("if(!measured)")&&room.Contains("ZWrite Off"),"room fallback/nonocclusion");
  Need(CinemaTiming.RealmGone==118.2f&&CinemaTiming.FounderGone==124&&CinemaTiming.EndTitleStart==124.25f,"farewell clock changed");
  Directory.CreateDirectory(Production43Review.Out);File.WriteAllText(Production43Review.Out+"/Combat38_Validation.txt","PASS charge acquisition, eight-second expiry, no stacking, one-hand release, one-second double damage, duplicate snapshot, tracking loss, stale pose, disconnect, reset, actual fireball block; zero/one/ten participant crescendo and fixed 98-101s finish; MR fallback and depth-write/occlusion guards. Native wearer and physical multiplayer acceptance pending.");Debug.Log("PRODUCTION38_COMBAT_PASS");
 }
}
