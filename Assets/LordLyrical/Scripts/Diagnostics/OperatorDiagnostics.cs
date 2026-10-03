using System;
using UnityEngine;
using UnityEngine.Profiling;
namespace LordLyrical
{
    public class OperatorDiagnostics:MonoBehaviour
    {
        public ExperienceDirector director;public NetworkSessionController session;
        float elapsed;int frames;double next;float fps;
        [Serializable] class Telemetry{public float fps,battery;public double driftMs,rttMs,memoryMb,time;public string phase,cue,error;}
        void Update()
        {
            elapsed+=Time.unscaledDeltaTime;frames++;if(elapsed>=1){fps=frames/elapsed;elapsed=0;frames=0;}
            if(AudioSettings.dspTime<next)return;next=AudioSettings.dspTime+2;
            var data=new Telemetry{fps=fps,battery=SystemInfo.batteryLevel,driftMs=director.audioClock.DriftSeconds*1000,rttMs=session.Clock.Ready?session.Clock.Rtt*1000:-1,memoryMb=Profiler.GetTotalAllocatedMemoryLong()/1048576d,time=director.ShowTime,phase=director.Phase.ToString(),cue=director.Scheduler==null?"":director.Scheduler.CurrentId,error=director.Error+session.Error};
            session.Report(JsonUtility.ToJson(data));
        }
        void OnGUI()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if(!director.configuration.operatorMode)return;
            GUI.Box(new Rect(12,12,460,130),"LORD LYRICAL · OPERATOR MONITOR");
            GUI.Label(new Rect(25,40,430,95),$"{director.Phase}  {director.ShowTime:F3}s  Slot {session.Slot+1}\n{fps:F1} FPS · audio drift {director.audioClock.DriftSeconds*1000:F1}ms · clock RTT {session.Clock.Rtt*1000:F1}ms\n{session.Manager?.ServerAddress} / {director.configuration.roomId}\n{director.Error}{session.Error}");
#endif
        }
    }
}
