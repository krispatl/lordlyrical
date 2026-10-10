using System;
using System.Globalization;
using Styly.NetSync;
using UnityEngine;
namespace LordLyrical
{
    public class NetworkSessionController:MonoBehaviour
    {
        public ExperienceConfiguration configuration;
        public bool offlineDemo;
        public bool RoomAligned {get;set;}
        public const string InputEpochKey="ll.inputEpoch";
        public string InputEpoch {get;private set;}=Guid.NewGuid().ToString("N");
        bool inputEpochPublished;
        public readonly SynchronizedClock Clock=new();
        public ShowSnapshot Snapshot{get;private set;}
        public Roster Participants{get;private set;}=new();
        public int Slot{get;private set;}=-1;
        public string Error{get;private set;}="";
        public double ServerNow=>Clock.Now(AudioSettings.dspTime);
        public bool Ready=>(offlineDemo||net!=null&&net.IsReady)&&Clock.Ready&&Slot>=0;
        public NetSyncManager Manager=>net;
        public event Action StateChanged;
        NetSyncManager net;double nextClock,nextJoin,lastState,clockResetAt;string boot="";
        static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        void Start()
        {
            if(offlineDemo){InitializeOffline();return;}
            net=NetSyncManager.Instance;
            if(net==null){Error="NetSyncManager missing";return;}
            net.OnRPCReceived.AddListener(OnRpc);net.OnGlobalVariableChanged.AddListener(OnGlobal);
            net.OnReady.AddListener(OnReady);net.OnVersionMismatch.AddListener(OnVersion);
            if(net.IsReady)OnReady();
        }
        void OnDestroy(){if(net==null)return;net.OnRPCReceived.RemoveListener(OnRpc);net.OnGlobalVariableChanged.RemoveListener(OnGlobal);net.OnReady.RemoveListener(OnReady);net.OnVersionMismatch.RemoveListener(OnVersion);}
        void OnVersion(string server,string client){Error=$"NetSync version mismatch: {server} / {client}";}
        void OnReady(){InputEpoch=Guid.NewGuid().ToString("N");inputEpochPublished=false;nextJoin=0;nextClock=0;ReadState(net.GetGlobalVariable("ll.show"));ReadRoster(net.GetGlobalVariable("ll.roster"));}
        void Update()
        {
            if(offlineDemo){AdvanceOffline(AudioSettings.dspTime,Application.isFocused);return;}
            if(net==null||!net.IsReady)return;
            if(!inputEpochPublished)inputEpochPublished=net.SetClientVariable(InputEpochKey,InputEpoch);
            double now=AudioSettings.dspTime;
            if(now>=nextClock){net.Rpc("ll.clock",new[]{now.ToString("R",Invariant)});nextClock=now+(Clock.Ready?1:.15);}
            if(now>=nextJoin){net.Rpc("ll.join",new[]{RoomAligned?"aligned":"unaligned"});nextJoin=now+2;ReadState(net.GetGlobalVariable("ll.show"));}
            if(lastState>0&&now-lastState>8)Error="Venue authority heartbeat missing; holding synchronized clock";
        }
        void OnGlobal(string name,string oldValue,string value){if(name=="ll.show")ReadState(value);else if(name=="ll.roster")ReadRoster(value);}
        [Serializable] class WireParticipant {public int s,c,a;public string d;}
        [Serializable] class WireRoster {public int v;public WireParticipant[] p;}
        public static bool TryDecodeRoster(string value,out Roster roster)
        {
            roster=null;if(string.IsNullOrEmpty(value))return false;
            try{
                var wire=JsonUtility.FromJson<WireRoster>(value);
                if(wire!=null&&wire.v==2&&wire.p!=null){
                    if(wire.p.Length>10)return false;int used=0;var rows=new ParticipantRecord[wire.p.Length];
                    for(int i=0;i<rows.Length;i++){
                        var p=wire.p[i];if(p==null||p.s<0||p.s>9||(used&(1<<p.s))!=0)return false;used|=1<<p.s;
                        rows[i]=new ParticipantRecord{slot=p.s,clientNo=p.c,deviceId=p.d,connected=p.a==1,key=RootKeys.Slots[p.s].key.ToString()};
                    }
                    roster=new Roster{participants=rows};return true;
                }
                var legacy=JsonUtility.FromJson<Roster>(value);if(legacy?.participants==null||legacy.participants.Length==0)return false;roster=legacy;return true;
            }catch(ArgumentException){return false;}
        }
        void ReadRoster(string value){if(TryDecodeRoster(value,out var roster))Participants=roster;else if(!string.IsNullOrEmpty(value))Error="Malformed roster";}
        void ReadState(string value)
        {
            if(string.IsNullOrEmpty(value))return;
            try
            {
                var s=JsonUtility.FromJson<ShowSnapshot>(value);
                if(s==null||!s.IsValid){Error="Invalid show state or protocol";return;}
                // Re-reading a cached state must not renew the authority heartbeat.
                if(Snapshot!=null&&s.bootId==Snapshot.bootId&&s.revision<=Snapshot.revision)return;
                if(boot!=s.bootId){boot=s.bootId;ResetClock();Slot=-1;nextJoin=0;}
                bool changed=Snapshot==null||s.runId!=Snapshot.runId||s.phase!=Snapshot.phase||s.epoch!=Snapshot.epoch;
                Snapshot=s;lastState=AudioSettings.dspTime;Error="";if(changed)StateChanged?.Invoke();
            }catch(ArgumentException){Error="Malformed show state";}
        }
        void OnRpc(int sender,string function,string[] args)
        {
            if(function=="ll.state"&&args.Length==1){ReadState(args[0]);return;}
            if(Snapshot==null||sender!=Snapshot.authorityClientNo)return;
            if(function=="ll.clock.reply"&&args.Length==4&&args[3]==boot&&double.TryParse(args[0],NumberStyles.Float,Invariant,out var t0)&&t0>=clockResetAt&&double.TryParse(args[1],NumberStyles.Float,Invariant,out var t1)&&double.TryParse(args[2],NumberStyles.Float,Invariant,out var t2))Clock.Sample(t0,t1,t2,AudioSettings.dspTime);
            else if(function=="ll.assignment"&&args.Length==2&&args[1]==boot&&int.TryParse(args[0],out var slot)&&slot>=0&&slot<10)Slot=slot;
            else if(function=="ll.error"&&args.Length>0)Error=args[0];
        }
        public void Report(string json){if(net!=null&&net.IsReady)net.Rpc("ll.telemetry",new[]{json});}
        public void InitializeOffline()
        {
            if(!offlineDemo)return;
            Slot=0;Error="";boot=Guid.NewGuid().ToString("N");ResetClock();
            Participants=new Roster{participants=new[]{new ParticipantRecord{slot=0,clientNo=-1,deviceId="offline-demo",key="D",connected=true}}};
            Snapshot=new ShowSnapshot{bootId=boot,revision=1,runId=0,phase="Ready",mode="Adaptive",sdk="0.17.4",protocol=8,authorityClientNo=-1,duration=121,openingDuration=17};
            StateChanged?.Invoke();Debug.Log("LL OFFLINE READY: align both fists to start; no server required");
        }
        public void AdvanceOffline(double now,bool focused)
        {
            if(!offlineDemo||Snapshot==null)return;
            if(Snapshot.phase=="Ready"&&RoomAligned&&focused){Snapshot.runId++;Snapshot.epoch=now+17;Snapshot.phase="Countdown";Snapshot.revision++;StateChanged?.Invoke();Debug.Log("LL OFFLINE START: alignment accepted");}
            if(Snapshot.phase=="Countdown"&&now>=Snapshot.epoch){Snapshot.phase="Running";Snapshot.revision++;}
            if(Snapshot.phase=="Running"&&now>=Snapshot.epoch+121){Snapshot.phase="Complete";Snapshot.revision++;}
        }
        public bool CanReplayOffline=>offlineDemo&&Snapshot!=null&&Snapshot.phase=="Complete"&&ServerNow>=Snapshot.epoch+CinemaTiming.OutroComplete;
        public void ReplayOffline(){if(!CanReplayOffline)return;Snapshot.phase="Ready";Snapshot.revision++;RoomAligned=true;StateChanged?.Invoke();}
        public void ResetClock(){Clock.Reset();clockResetAt=AudioSettings.dspTime;nextClock=clockResetAt;nextJoin=0;if(offlineDemo)for(int i=0;i<3;i++)Clock.Sample(clockResetAt,clockResetAt,clockResetAt,clockResetAt);}
    }
}
