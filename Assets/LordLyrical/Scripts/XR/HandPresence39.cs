using System;
using UnityEngine;
using Styly.NetSync;
namespace LordLyrical {
 [Serializable] public sealed class HandPresencePacket39 {public string epoch;public int run,slot,sequence,tracked,open;}
 public sealed class HandPresence39:MonoBehaviour {
  public const float Expiry=.30f;
  static HandPresence39 instance;
  readonly HandPresencePacket39[] remote=new HandPresencePacket39[10];readonly float[] received=new float[10];readonly int[] owners=new int[10];readonly ParticipantInputSequence sequences=new();
  TrackedRig rig;NetworkSessionController session;NetSyncManager net;int serial,lastMask=-1,lastOpen=-1,previousSlot=-1,previousRun=-1;float sent=-100;string previousEpoch;
  void OnEnable(){instance=this;rig=GetComponent<TrackedRig>();Array.Fill(owners,-1);}
  public static bool Valid(HandPresencePacket39 p)=>p!=null&&p.slot>=0&&p.slot<10&&p.sequence>0&&p.tracked>=0&&p.tracked<=3&&p.open>=0&&p.open<=3&&(p.open&~p.tracked)==0;
  public static bool Fresh(float now,float last)=>now>=last&&now-last<Expiry;
  static int Mask(int slot,bool palms){
   var s=instance;if(!s||!s.session||slot<0||slot>=10)return 0;
   if(slot==s.session.Slot){if(!Application.isFocused||!s.rig||!s.rig.Hands)return 0;var h=s.rig.Hands;return (h.Left.tracked&&(!palms||h.Left.open)?1:0)|(h.Right.tracked&&(!palms||h.Right.open)?2:0);}
   var p=s.remote[slot];if(p==null||!Fresh(Time.unscaledTime,s.received[slot])||!s.net||ParticipantInputSequence.OwnerOf(s.session.Participants,slot)!=s.owners[slot]||s.net.GetClientVariable(NetworkSessionController.InputEpochKey,s.owners[slot])!=p.epoch||p.run!=(s.session.Snapshot?.runId??0))return 0;
   return palms?p.open:p.tracked;
  }
  public static bool Tracked(int slot,bool left)=>(Mask(slot,false)&(left?1:2))!=0;
  public static bool Open(int slot,bool left)=>(Mask(slot,true)&(left?1:2))!=0;
  void Update(){
   if(!session)session=FindFirstObjectByType<NetworkSessionController>();if(!session)return;
   if(!net&&session.Manager){net=session.Manager;net.OnRPCReceived.AddListener(Rpc);}
   int run=session.Snapshot?.runId??0;if(previousRun!=run||previousSlot!=session.Slot||previousEpoch!=session.InputEpoch){Array.Clear(remote,0,remote.Length);previousRun=run;previousSlot=session.Slot;previousEpoch=session.InputEpoch;lastMask=lastOpen=-1;}
   Publish(Application.isFocused);
  }
  void Publish(bool focused){
   if(!session||session.Slot<0||session.Slot>=10||!net||!net.IsReady)return;
   int tracked=0,open=0;if(focused&&rig&&rig.Hands){var h=rig.Hands;tracked=(h.Left.tracked?1:0)|(h.Right.tracked?2:0);open=(h.Left.tracked&&h.Left.open?1:0)|(h.Right.tracked&&h.Right.open?2:0);}
   if(tracked==lastMask&&open==lastOpen&&Time.unscaledTime-sent<.10f)return;
   var p=new HandPresencePacket39{epoch=session.InputEpoch,run=session.Snapshot?.runId??0,slot=session.Slot,sequence=++serial,tracked=tracked,open=open};net.Rpc("ll.hands.presence39",new[]{JsonUtility.ToJson(p)});sent=Time.unscaledTime;lastMask=tracked;lastOpen=open;
  }
  void Rpc(int sender,string method,string[] args){
   if(method!="ll.hands.presence39"||!net||sender==net.ClientNo||args==null||args.Length!=1||args[0].Length>1200)return;
   try{var p=JsonUtility.FromJson<HandPresencePacket39>(args[0]);if(!Valid(p)||p.run!=(session.Snapshot?.runId??0)||net.GetClientVariable("ll.concert",sender)!="10")return;
    int owner=ParticipantInputSequence.OwnerOf(session.Participants,p.slot);if(!sequences.TryAccept(session.Snapshot?.runId??0,p.run,p.slot,sender,owner,p.epoch,net.GetClientVariable(NetworkSessionController.InputEpochKey,sender),p.sequence))return;
    remote[p.slot]=p;owners[p.slot]=sender;received[p.slot]=Time.unscaledTime;
   }catch(ArgumentException){}
  }
  void OnApplicationFocus(bool focus){if(!focus){Publish(false);Array.Clear(remote,0,remote.Length);}}
  void OnApplicationPause(bool paused){if(paused)Publish(false);}
  void OnDisable(){Publish(false);if(net)net.OnRPCReceived.RemoveListener(Rpc);net=null;Array.Clear(remote,0,remote.Length);if(instance==this)instance=null;}
 }
}
