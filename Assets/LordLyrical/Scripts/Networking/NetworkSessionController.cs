using System;
using System.Globalization;
using Styly.NetSync;
using UnityEngine;
namespace LordLyrical
{
    public class NetworkSessionController:MonoBehaviour
    {
        public ExperienceConfiguration configuration;
        public readonly SynchronizedClock Clock=new();
        public ShowSnapshot Snapshot{get;private set;}
        public Roster Participants{get;private set;}=new();
        public int Slot{get;private set;}=-1;
        public string Error{get;private set;}="";
        public double ServerNow=>Clock.Now(AudioSettings.dspTime);
        public bool Ready=>net!=null&&net.IsReady&&Clock.Ready&&Slot>=0;
        public NetSyncManager Manager=>net;
        public event Action StateChanged;
        NetSyncManager net;double nextClock,nextJoin,lastState,clockResetAt;string boot="";
        static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        void Start()
        {
            net=NetSyncManager.Instance;
            if(net==null){Error="NetSyncManager missing";return;}
            net.OnRPCReceived.AddListener(OnRpc);net.OnGlobalVariableChanged.AddListener(OnGlobal);
            net.OnReady.AddListener(OnReady);net.OnVersionMismatch.AddListener(OnVersion);
            if(net.IsReady)OnReady();
        }
        void OnDestroy(){if(net==null)return;net.OnRPCReceived.RemoveListener(OnRpc);net.OnGlobalVariableChanged.RemoveListener(OnGlobal);net.OnReady.RemoveListener(OnReady);net.OnVersionMismatch.RemoveListener(OnVersion);}
        void OnVersion(string server,string client){Error=$"NetSync version mismatch: {server} / {client}";}
        void OnReady(){nextJoin=0;nextClock=0;ReadState(net.GetGlobalVariable("ll.show"));ReadRoster(net.GetGlobalVariable("ll.roster"));}
        void Update()
        {
            if(net==null||!net.IsReady)return;double now=AudioSettings.dspTime;
            if(now>=nextClock){net.Rpc("ll.clock",new[]{now.ToString("R",Invariant)});nextClock=now+(Clock.Ready?1:.15);}
            if(now>=nextJoin){net.Rpc("ll.join");nextJoin=now+2;ReadState(net.GetGlobalVariable("ll.show"));}
            if(lastState>0&&now-lastState>8)Error="Venue authority heartbeat missing; holding synchronized clock";
        }
        void OnGlobal(string name,string oldValue,string value){if(name=="ll.show")ReadState(value);else if(name=="ll.roster")ReadRoster(value);}
        void ReadRoster(string value){if(string.IsNullOrEmpty(value))return;try{var r=JsonUtility.FromJson<Roster>(value);if(r!=null&&r.participants!=null)Participants=r;}catch(ArgumentException){Error="Malformed roster";}}
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
        public void ResetClock(){Clock.Reset();clockResetAt=AudioSettings.dspTime;nextClock=clockResetAt;nextJoin=0;}
    }
}
