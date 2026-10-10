using System;
using UnityEngine;
using Styly.NetSync;
namespace LordLyrical {
 [Serializable] public sealed class ShieldPacket37 {
  public string epoch;public int run,slot,sequence;public float time;public bool left,right,leftTracked,rightTracked;public Vector3 leftPosition,rightPosition;
 }
 public sealed class SharedFistShields37:MonoBehaviour {
  readonly FistShield36[] visuals=new FistShield36[10];readonly ShieldPacket37[] poses=new ShieldPacket37[10];
  readonly float[] received=new float[10];readonly int[] owners=new int[10];readonly ParticipantInputSequence sequences=new();
  NetworkSessionController session;ConcertEncounter encounter;NetSyncManager net;TrackedRig rig;float clock,lastSend;int run=-1,serial;bool live,wasClosed;int previousSlot=-1;string previousEpoch;
  public void Initialize(NetworkSessionController s,ConcertEncounter e){session=s;encounter=e;for(int i=0;i<10;i++){var g=new GameObject("Shared shield / slot "+i);g.transform.SetParent(transform,false);visuals[i]=g.AddComponent<FistShield36>();visuals[i].Initialize();owners[i]=-1;}}
  public static bool Valid(ShieldPacket37 p,Vector3 head)=>p!=null&&p.slot>=0&&p.slot<10&&float.IsFinite(p.time)&&ManualResonanceBeams.Finite(head)&&(!(p.left||p.leftTracked)||ManualResonanceBeams.Finite(p.leftPosition)&&Vector3.Distance(head,p.leftPosition)<1.8f)&&(!(p.right||p.rightTracked)||ManualResonanceBeams.Finite(p.rightPosition)&&Vector3.Distance(head,p.rightPosition)<1.8f);
  public void Render(float t,bool active){
   if(previousSlot!=session.Slot||previousEpoch!=session.InputEpoch){if(previousSlot>=0)Counterattack38.Cancel(encounter.State.counters[previousSlot]);previousSlot=session.Slot;previousEpoch=session.InputEpoch;Array.Clear(poses,0,poses.Length);}
   clock=t;int current=session.Snapshot?.runId??0;live=active&&session.Ready&&t>=26&&t<101;
   if(!rig)rig=FindFirstObjectByType<TrackedRig>();if(!net&&session.Manager){net=session.Manager;net.OnRPCReceived.AddListener(Rpc);}
   if(current!=run){run=current;Array.Clear(poses,0,poses.Length);wasClosed=false;}
   bool closed=false;
   if(session.Slot>=0&&session.Slot<10){
    var l=live&&rig&&rig.Hands?rig.Hands.Left:default;var r=live&&rig&&rig.Hands?rig.Hands.Right:default;
    var p=new ShieldPacket37{epoch=session.InputEpoch,run=run,slot=session.Slot,time=t,sequence=++serial,leftTracked=l.tracked,rightTracked=r.tracked,left=l.tracked&&l.closed,right=r.tracked&&r.closed,leftPosition=l.position,rightPosition=r.position};
    closed=p.left||p.right;poses[p.slot]=p;received[p.slot]=Time.unscaledTime;owners[p.slot]=net?net.ClientNo:-1;
    if(net&&net.IsReady&&(closed!=wasClosed||live&&Time.unscaledTime-lastSend>.10f)){lastSend=Time.unscaledTime;net.Rpc("ll.concert.shields37",new[]{JsonUtility.ToJson(p)});}
   }
   wasClosed=closed;
   for(int slot=0;slot<10;slot++){
    var p=poses[slot];bool local=slot==session.Slot;
    bool fresh=live&&p!=null&&Time.unscaledTime-received[slot]<.35f;
    if(!local&&fresh)fresh=net&&ParticipantInputSequence.OwnerOf(session.Participants,slot)==owners[slot]&&net.GetClientVariable(NetworkSessionController.InputEpochKey,owners[slot])==p.epoch;
    Vector3 head=Camera.main?Camera.main.transform.position:Vector3.zero;
    if(!local&&fresh)fresh=NetworkRoleAvatar.TryGetHead(slot,out head);
    var l=new HandPoseInput.HandState{tracked=fresh&&p.leftTracked,closed=fresh&&p.left,position=p!=null?p.leftPosition:Vector3.zero};
    var r=new HandPoseInput.HandState{tracked=fresh&&p.rightTracked,closed=fresh&&p.right,position=p!=null?p.rightPosition:Vector3.zero};
    visuals[slot].Charge(encounter.State.counters[slot].expires>t);visuals[slot].RenderPose(t,live,l,r,RootKeys.Slots[slot].color,head,Time.unscaledDeltaTime);
    encounter.SetShield(slot,t,head,l,r,visuals[slot].Merged);
    foreach(var shot in encounter.State.dragonShots)if(shot.resolved&&shot.blocked&&shot.blockerSlot==slot&&shot.impactTime>0)visuals[slot].Impact(shot.impactTime,shot.impactPoint);
   }
  }
  void Rpc(int sender,string method,string[] args){
   if(net&&sender!=net.ClientNo&&net.GetClientVariable("ll.concert",sender)!="10")return;
   if(method!="ll.concert.shields37"||!live||args==null||args.Length!=1||args[0].Length>2500||sender==net.ClientNo)return;
   try{var p=JsonUtility.FromJson<ShieldPacket37>(args[0]);if(p==null||p.slot<0||p.slot>=10||p.run!=run||Mathf.Abs(p.time-clock)>.75f||ParticipantInputSequence.OwnerOf(session.Participants,p.slot)!=sender||!NetworkRoleAvatar.TryGetHead(p.slot,out var head)||!Valid(p,head))return;
    if(!sequences.TryAccept(run,p.run,p.slot,sender,sender,p.epoch,net.GetClientVariable(NetworkSessionController.InputEpochKey,sender),p.sequence))return;
    if(owners[p.slot]!=sender||poses[p.slot]!=null&&poses[p.slot].epoch!=p.epoch)Counterattack38.Cancel(encounter.State.counters[p.slot]);poses[p.slot]=p;owners[p.slot]=sender;received[p.slot]=Time.unscaledTime;
   }catch(ArgumentException){}
  }
  void OnApplicationFocus(bool focused){if(!focused){
   if(net&&net.IsReady&&session.Slot>=0)net.Rpc("ll.concert.shields37",new[]{JsonUtility.ToJson(new ShieldPacket37{epoch=session.InputEpoch,run=run,slot=session.Slot,time=clock,sequence=++serial})});
   live=false;wasClosed=false;Array.Clear(poses,0,poses.Length);
  }}
  void OnDestroy(){if(net)net.OnRPCReceived.RemoveListener(Rpc);}
 }
}
