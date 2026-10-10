using System;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
namespace LordLyrical
{
    [DefaultExecutionOrder(30000)]
    public class OperatorDiagnostics:MonoBehaviour
    {
        public ExperienceDirector director;public NetworkSessionController session;
        float elapsed;int frames;double next;float fps;
        [Serializable] class Telemetry{public float fps,battery,worldRenderMs,gpuFrameMs,vocalActivity,mainThreadMs,renderThreadMs,drawCalls,triangles;public int vfxQuality,activeParticles,manualBeams;public bool handPermissionGranted,handPermissionPending,handSubsystemRunning,leftHandTracked,rightHandTracked,leftPalmOpen,rightPalmOpen,leftNativeHandTracked,rightNativeHandTracked;public int leftValidFingers,rightValidFingers,handLayoutJoints,handUpdateFlags;public bool applicationFocused,handExtensionEnabled;public bool leftHandMeshVisible,rightHandMeshVisible;public int leftHandMeshJoints,rightHandMeshJoints;public float handMeshJointError;public string inputMode;public double driftMs,rttMs,memoryMb,time;public string phase,cue,error,openingStage;public bool passthroughSupported,passthroughActive,roomAligned,cameraHdr,cameraPostProcessing,projectionLayerReady;public float roomOpacity,cameraBackgroundAlpha;public string compositionProvider,projectionAlphaProbe;public bool nativeSourceAlphaApplied;public int arrivalParticles;public string roomScanStatus;public bool roomMeshSupported,roomSceneSupported,spatialPermission;public int roomSurfaces,roomVertices,cinemaParticles,cooperativeBursts;public float realmDissolve;public int beamDamageHits,orcsDefeated,impactParticles;public float rescueCharge,finaleCharge;public int concertScore,concertBlocks,concertHits,dragonOwners,trackedAvatars,trackedSlotMask,animationVersion;public float harmony,faceViseme,orcWalkMotion,orcAttackMotion,jeruLegMotion,jeruArmMotion,dragonWingMotion,dragonTailMotion;public int animationConflicts;}
        FinalPoseMotion finalMotion;
        readonly System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem> displays=new();
        double nextGpuAttempt;
        CinematicParticleDirector showParticles;
        ProfilerRecorder mainThreadRecorder,renderThreadRecorder,drawCallsRecorder,trianglesRecorder;
        double mainSum,threadSum,drawSum,triangleSum;int mainSamples,threadSamples,drawSamples,triangleSamples;
        static ProfilerRecorder StartCounter(ProfilerCategory category,string name)
        {
            try{return ProfilerRecorder.StartNew(category,name,1);}
            catch(Exception){return default;}
        }
        void OnEnable()
        {
            mainThreadRecorder=StartCounter(ProfilerCategory.Internal,"Main Thread");
            renderThreadRecorder=StartCounter(ProfilerCategory.Internal,"Render Thread");
            drawCallsRecorder=StartCounter(ProfilerCategory.Render,"Draw Calls Count");
            trianglesRecorder=StartCounter(ProfilerCategory.Render,"Triangles Count");
        }
        void OnDisable()
        {
            mainThreadRecorder.Dispose();renderThreadRecorder.Dispose();drawCallsRecorder.Dispose();trianglesRecorder.Dispose();
        }
        void OnApplicationPause(bool paused){if(!paused)nextGpuAttempt=0;}
        static void Accumulate(ProfilerRecorder recorder,ref double sum,ref int samples)
        {
            if(recorder.Valid&&recorder.Count>0){sum+=recorder.LastValue;samples++;}
        }
        float renderSum,gpuSum;int renderSamples,gpuSamples;
        void LateUpdate()
        {
            if(finalMotion==null&&director.world)finalMotion=new FinalPoseMotion(director.world);finalMotion?.Sample();
            renderSum+=director.WorldRenderMilliseconds;renderSamples++;
            if(displays.Count==0)SubsystemManager.GetSubsystems(displays);
            if(Time.realtimeSinceStartupAsDouble>=nextGpuAttempt)foreach(var display in displays)if(display.running){if(display.TryGetAppGPUTimeLastFrame(out float gpu)&&gpu>0){gpuSum+=gpu*1000;gpuSamples++;}else nextGpuAttempt=Time.realtimeSinceStartupAsDouble+2;break;}
            Accumulate(mainThreadRecorder,ref mainSum,ref mainSamples);Accumulate(renderThreadRecorder,ref threadSum,ref threadSamples);
            Accumulate(drawCallsRecorder,ref drawSum,ref drawSamples);Accumulate(trianglesRecorder,ref triangleSum,ref triangleSamples);
            elapsed+=Time.unscaledDeltaTime;frames++;if(elapsed>=1){fps=frames/elapsed;elapsed=0;frames=0;}
            if(AudioSettings.dspTime<next)return;next=AudioSettings.dspTime+2;
            var data=new Telemetry{fps=fps,battery=SystemInfo.batteryLevel,driftMs=director.audioClock.DriftSeconds*1000,rttMs=session.Clock.Ready?session.Clock.Rtt*1000:-1,memoryMb=Profiler.GetTotalAllocatedMemoryLong()/1048576d,time=director.ShowTime,phase=director.Phase.ToString(),cue=director.Scheduler==null?"":director.Scheduler.CurrentId,error=director.Error+session.Error};
            var tracked=FindFirstObjectByType<TrackedRig>();if(tracked&&tracked.Hands){data.applicationFocused=Application.isFocused;data.handExtensionEnabled=tracked.Hands.NativeExtensionEnabled;data.handLayoutJoints=tracked.Hands.SupportedJointCount;data.handUpdateFlags=tracked.Hands.NativeUpdateFlags;data.handPermissionGranted=tracked.Hands.PermissionGranted;data.handPermissionPending=tracked.Hands.PermissionRequestPending;data.handSubsystemRunning=tracked.Hands.Available;data.inputMode="HandsOnly";data.leftHandMeshVisible=tracked.LeftVisual&&tracked.LeftVisual.Visible;data.rightHandMeshVisible=tracked.RightVisual&&tracked.RightVisual.Visible;data.leftHandMeshJoints=tracked.LeftVisual?tracked.LeftVisual.AppliedJointCount:0;data.rightHandMeshJoints=tracked.RightVisual?tracked.RightVisual.AppliedJointCount:0;data.handMeshJointError=Mathf.Max(tracked.LeftVisual?tracked.LeftVisual.JointPositionError:0,tracked.RightVisual?tracked.RightVisual.JointPositionError:0);data.leftNativeHandTracked=tracked.Hands.LeftNativeTracked;data.rightNativeHandTracked=tracked.Hands.RightNativeTracked;data.leftValidFingers=tracked.Hands.Left.validFingers;data.rightValidFingers=tracked.Hands.Right.validFingers;data.leftHandTracked=tracked.Hands.Left.tracked;data.rightHandTracked=tracked.Hands.Right.tracked;data.leftPalmOpen=tracked.Hands.Left.open;data.rightPalmOpen=tracked.Hands.Right.open;}
            if(director.world){var manual=director.world.GetComponent<ManualResonanceBeams>();if(manual)data.manualBeams=manual.ActiveBeams;}
            if(director.world){var c=director.world.GetComponent<ConcertInteraction>();if(c&&c.Encounter!=null){var s=c.Encounter.State;data.concertScore=s.score;data.cooperativeBursts=s.cooperativeBursts;data.concertBlocks=s.blocks;data.concertHits=s.hits;data.harmony=s.harmony;data.beamDamageHits=s.beamHits;data.orcsDefeated=s.defeated;data.rescueCharge=s.rescueCharge;data.finaleCharge=s.finaleCharge;}
                foreach(var tr in director.world.GetComponentsInChildren<Transform>(true))if(tr.name=="Current CyberDragon | chamber performance")data.dragonOwners++;
                var face=director.world.GetComponentInChildren<JeruConcertFace>();if(face)data.vocalActivity=face.VocalActivity;if(face)foreach(var skin in face.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.sharedMesh){foreach(string shape in new[]{"viseme_PP","viseme_ou","viseme_E","viseme_aa"}){int index=skin.sharedMesh.GetBlendShapeIndex(shape);if(index>=0)data.faceViseme=Mathf.Max(data.faceViseme,skin.GetBlendShapeWeight(index));}}
            }
            data.trackedAvatars=NetworkRoleAvatar.TrackedCount;data.trackedSlotMask=NetworkRoleAvatar.TrackedSlotMask;data.animationVersion=34;
            data.vfxQuality=director.configuration?director.configuration.vfxQuality:-1;
            if(!showParticles&&director.world)showParticles=director.world.GetComponentInChildren<CinematicParticleDirector>();
            data.activeParticles=showParticles?showParticles.ActiveParticles:0;var impacts=director.world?director.world.GetComponent<CombatImpactVFX>():null;if(impacts){data.impactParticles=impacts.ActiveParticles;data.activeParticles+=data.impactParticles;}
            // Thread markers include time spent waiting; they are not pure CPU work.
            data.mainThreadMs=mainSamples>0?(float)(mainSum/mainSamples/1000000):-1;
            data.renderThreadMs=threadSamples>0?(float)(threadSum/threadSamples/1000000):-1;
            data.drawCalls=drawSamples>0?(float)(drawSum/drawSamples):-1;
            data.triangles=triangleSamples>0?(float)(triangleSum/triangleSamples):-1;
            mainSum=threadSum=drawSum=triangleSum=0;mainSamples=threadSamples=drawSamples=triangleSamples=0;
            data.roomAligned=session.RoomAligned;data.nativeSourceAlphaApplied=PassthroughProjectionLayer.NativeAlphaApplied;data.projectionAlphaProbe=PassthroughProjectionLayer.AlphaProbe;
            if(director.Opening!=null){data.openingStage=director.Opening.Stage;data.passthroughSupported=director.Opening.Supported;data.passthroughActive=director.Opening.PassthroughActive;data.roomOpacity=director.Opening.RoomOpacity;data.arrivalParticles=director.Opening.ParticleCount;data.cameraHdr=director.Opening.CameraHdr;data.cameraPostProcessing=director.Opening.CameraPostProcessing;data.cameraBackgroundAlpha=director.Opening.CameraBackgroundAlpha;data.compositionProvider=director.Opening.CompositionProvider;data.projectionLayerReady=director.Opening.ProjectionLayerReady;
                var cinema=director.Opening.Cinema;
                if(cinema!=null){var room=cinema.Survey;data.roomScanStatus=room.Status;data.roomMeshSupported=room.MeshSupported;data.roomSceneSupported=room.SceneSupported;data.spatialPermission=room.PermissionGranted;data.roomSurfaces=room.SurfaceCount;data.roomVertices=room.VertexCount;data.cinemaParticles=cinema.ActiveParticles;data.realmDissolve=cinema.RealmRelease;data.activeParticles+=cinema.ActiveParticles;}}
            data.worldRenderMs=renderSamples>0?renderSum/renderSamples:0;data.gpuFrameMs=gpuSamples>0?gpuSum/gpuSamples:-1;
            renderSum=gpuSum=0;renderSamples=gpuSamples=0;
            if(finalMotion!=null){data.orcWalkMotion=finalMotion.OrcWalk;data.orcAttackMotion=finalMotion.OrcAttack;data.jeruLegMotion=finalMotion.JeruLeg;data.jeruArmMotion=finalMotion.JeruArm;data.dragonWingMotion=finalMotion.DragonWing;data.dragonTailMotion=finalMotion.DragonTail;data.animationConflicts=finalMotion.Conflicts;finalMotion.Clear();}
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

