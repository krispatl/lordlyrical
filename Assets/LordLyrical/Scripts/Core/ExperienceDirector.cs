using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;
namespace LordLyrical
{
    [DefaultExecutionOrder(25)]
    public class ExperienceDirector:MonoBehaviour
    {
        public ExperienceConfiguration configuration;public NetworkSessionController session;
        public AudioMasterClock audioClock;public ResonantWorld world;public HapticsDirector haptics;
        public SessionPhase Phase{get;private set;}=SessionPhase.Boot;
        public double ShowTime{get;private set;}
        public CueScheduler Scheduler{get;private set;}
        public string Error{get;private set;}="";
        public float WorldRenderMilliseconds {get;private set;}
        public PassthroughOpening Opening {get;private set;}
        int activeRun=-1;string activeBoot="";bool initialized;SpatialAccents accents;RecordedVoiceDirector voices;
        IEnumerator Start()
        {
            if(configuration==null){Fail("Experience configuration missing");yield break;}
            if(!configuration.Validate(out var error)){Fail(error);yield break;}
#if UNITY_ANDROID && !UNITY_EDITOR
            Phase=SessionPhase.InitializingXR;float deadline=Time.realtimeSinceStartup+20;
            while((XRGeneralSettings.Instance==null||XRGeneralSettings.Instance.Manager.activeLoader==null)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(XRGeneralSettings.Instance==null||XRGeneralSettings.Instance.Manager.activeLoader==null){Fail("OpenXR initialization failed; check PICO loader and runtime");yield break;}
#endif
            try{Scheduler=new CueScheduler(JsonUtility.FromJson<CueTable>(configuration.cueJson.text).cues);}
            catch(Exception e){Fail(e.Message);yield break;}
            Scheduler.Fired+=OnCue;world.Initialize(configuration,session);Opening=new PassthroughOpening(world,configuration,Camera.main);var tracked=FindFirstObjectByType<TrackedRig>();if(tracked){var alignment=tracked.gameObject.GetComponent<SharedRoomAlignment>();if(!alignment)alignment=tracked.gameObject.AddComponent<SharedRoomAlignment>();alignment.rig=tracked;alignment.director=this;}
            accents=gameObject.AddComponent<SpatialAccents>();accents.enabled=configuration.spatialAccents;
            voices=gameObject.AddComponent<RecordedVoiceDirector>();voices.Initialize(configuration.voiceCues,JsonUtility.FromJson<CueTable>(configuration.cueJson.text).cues,world);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            gameObject.AddComponent<DevelopmentCapture>();
#endif
            Application.targetFrameRate=configuration.targetRefreshRate;
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            AudioSettings.GetDSPBufferSize(out var size,out var count);
            Debug.Log($"LL boot: track={configuration.track.length:F3}s, DSP buffer={size}x{count}, target={Application.targetFrameRate}Hz");
            Application.lowMemory+=LowMemory;AudioSettings.OnAudioConfigurationChanged+=AudioConfigurationChanged;Phase=SessionPhase.ConnectingNetwork;initialized=true;
            yield break;
        }
        void OnDestroy(){Opening?.Dispose();Application.lowMemory-=LowMemory;AudioSettings.OnAudioConfigurationChanged-=AudioConfigurationChanged;if(Scheduler!=null)Scheduler.Fired-=OnCue;}
        void LowMemory(){Debug.LogWarning("LL memory pressure: lowering VFX detail");configuration.vfxQuality=0;}
        void Fail(string message){Error=message;Phase=SessionPhase.Error;Debug.LogError("LL: "+message);if(audioClock!=null)audioClock.Stop();}
        // Render after Unity animation, then let SALSA QueueProcessor (50) apply speech and face (300) apply expressions.
        void LateUpdate()
        {
            if(!initialized)return;
            var snapshot=session.Snapshot;
            if(snapshot==null||!session.Ready)
            {
                // A brief network loss retains the locked clock and running show.
                if(activeRun<0){Phase=SessionPhase.WaitingForAssignment;RenderWorld(ShowFrame.At(0),false);return;}
            }
            if(snapshot==null)return;
            bool active=snapshot.phase=="Countdown"||snapshot.phase=="Running"||snapshot.phase=="Complete";
            if(!active)
            {
                if(activeRun>=0){audioClock.Stop();haptics.Stop();accents.Stop();voices.Stop();Scheduler.Reset();activeRun=-1;world.ResetEffects();}
                Phase=SessionPhase.Ready;ShowTime=0;RenderWorld(ShowFrame.At(0),false);return;
            }
            if(!session.Clock.Ready)return;
            double target=session.ServerNow-snapshot.epoch;
            if(activeRun!=snapshot.runId||activeBoot!=snapshot.bootId)
            {
                activeRun=snapshot.runId;activeBoot=snapshot.bootId;world.ResetEffects();haptics.Stop();audioClock.Schedule(target);
                if(target>0)Scheduler.Seek(target);else Scheduler.Reset();
                Debug.Log($"LL reconstructed run={activeRun} at {Math.Max(0,target):F3}s slot={session.Slot+1}");
            }
            audioClock.Tick(target);
            ShowTime=Math.Clamp(audioClock.Scheduled?audioClock.TimeSeconds:target,0,121);
            Phase=target<0?SessionPhase.Countdown:target>=121?SessionPhase.Complete:target>=111?SessionPhase.Ending:SessionPhase.Running;
            if(target>=0){Scheduler.Advance(ShowTime);RenderWorld(ShowFrame.At(ShowTime),true,target);voices.Tick(ShowTime);}
            else RenderWorld(ShowFrame.At(0),false,target);
        }
        void RenderWorld(ShowFrame frame,bool live,double signedSongTime=double.NegativeInfinity)
        {
            double start=Time.realtimeSinceStartupAsDouble;world.Render(frame,live);Opening?.Render(signedSongTime);
            WorldRenderMilliseconds=(float)((Time.realtimeSinceStartupAsDouble-start)*1000);
        }
        void OnCue(Cue cue)
        {
            if(accents!=null&&accents.enabled)accents.Cue(cue);
            haptics.Cue(cue,session.Slot);
            if(configuration.debugLogging)Debug.Log($"LL CUE {cue.id} expected={cue.time:F3} actual={ShowTime:F3} errorMs={(ShowTime-cue.time)*1000:F1}");
        }
        public void Preview(double time){if(!initialized)return;audioClock.Stop();Scheduler.Seek(time);ShowTime=time;RenderWorld(ShowFrame.At(time),true,time);}
        void OnApplicationPause(bool paused){RecoverAudioClock();}
        void AudioConfigurationChanged(bool deviceChanged){RecoverAudioClock();}
        void RecoverAudioClock(){if(!initialized)return;audioClock.Stop();haptics.Stop();accents.Stop();voices.Stop();session.ResetClock();activeRun=-1;}
    }
}
