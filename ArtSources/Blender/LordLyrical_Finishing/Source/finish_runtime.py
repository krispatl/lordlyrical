from pathlib import Path
import shutil
root=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\als\outputs\LordLyrical');out=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs');backup=out/'LordLyrical_Finishing/Integration_Backup'
def edit(path,pairs):
 p=root/path;b=backup/path;b.parent.mkdir(parents=True,exist_ok=True)
 if not b.exists():shutil.copy2(p,b)
 s=p.read_text(encoding='utf-8-sig')
 for old,new in pairs:
  if old not in s:raise ValueError(path+' missing '+old[:60])
  s=s.replace(old,new,1)
 p.write_text(s,encoding='utf-8')
edit('Assets/LordLyrical/Scripts/Core/ExperienceDirector.cs',[
('bool initialized;SpatialAccents accents;','bool initialized;SpatialAccents accents;RecordedVoiceDirector voices;'),
('accents.enabled=configuration.spatialAccents;','accents.enabled=configuration.spatialAccents;\n            voices=gameObject.AddComponent<RecordedVoiceDirector>();voices.Initialize(configuration.voiceCues,JsonUtility.FromJson<CueTable>(configuration.cueJson.text).cues,world);'),
('haptics.Stop();accents.Stop();Scheduler.Reset();','haptics.Stop();accents.Stop();voices.Stop();Scheduler.Reset();'),
('world.Render(ShowFrame.At(ShowTime),true);','world.Render(ShowFrame.At(ShowTime),true);voices.Tick(ShowTime);'),
('if(cue.category=="Impact"||cue.id=="amplitude-rise")haptics.Pulse(cue.id=="the-word"?.6f:.3f,.18f);','haptics.Cue(cue,session.Slot);'),
('haptics.Stop();accents.Stop();session.ResetClock();','haptics.Stop();accents.Stop();voices.Stop();session.ResetClock();')])
edit('Assets/LordLyricalAvatars_Unity/Scripts/ParticipantAvatarPerformance.cs',[
('public LODGroup bodyLod;','public LODGroup bodyLod,handLod;\n        public SkinnedMeshRenderer[] distanceHands=Array.Empty<SkinnedMeshRenderer>();'),
('Glow(energyMesh,2);Glow(leftHandMesh,.25f);Glow(rightHandMesh,.25f);','Glow(energyMesh,2);Glow(leftHandMesh,.25f);Glow(rightHandMesh,.25f);foreach(var r in distanceHands)Glow(r,.25f);'),
('if(bodyLod)bodyLod.enabled=!firstPerson&&!hidden;','if(bodyLod)bodyLod.enabled=!firstPerson&&!hidden;\n            if(handLod)handLod.enabled=!firstPerson&&!hidden;\n            foreach(var hand in distanceHands)if(hand)hand.enabled=!firstPerson&&!hidden;')])
edit('Assets/LordLyricalJeru_Unity/Scripts/JeruPerformance.cs',[
('MaterialPropertyBlock block;','MaterialPropertyBlock block;FacialLodRelay distance;'),
('            graph.Evaluate(0);','            graph.Evaluate(0);if(!distance)distance=GetComponent<FacialLodRelay>();if(distance)distance.Synchronize();')])
edit('Assets/LordLyricalOrc_Unity/Scripts/SonicOrcPerformance.cs',[
('else SampleClip(1,time-26+index*.075f);','else if(time<28)SampleClip(2,Mathf.Max(0,time-26));\n            else SampleClip(1,time-28+index*.075f);')])
for path in ['Assets/LordLyrical/Scripts/Audio/SpatialAccents.cs','Assets/LordLyrical/Scripts/XR/HapticsDirector.cs']:
 p=root/path;b=backup/path;b.parent.mkdir(parents=True,exist_ok=True)
 if not b.exists():shutil.copy2(p,b)
(root/'Assets/LordLyrical/Scripts/Audio/SpatialAccents.cs').write_text('''using UnityEngine;
namespace LordLyrical
{
    public sealed class SpatialAccents:MonoBehaviour
    {
        [Range(0,1)]public float mix=.09f;
        readonly AudioSource[] pool=new AudioSource[6];AudioClip foot,fork,glass,wing,growl,word;int next;
        void Awake(){
            foot=Resources.Load<AudioClip>("LordLyricalAccents/RitualFootfall");fork=Resources.Load<AudioClip>("LordLyricalAccents/TuningForkImpact");glass=Resources.Load<AudioClip>("LordLyricalAccents/GlassResonance");wing=Resources.Load<AudioClip>("LordLyricalAccents/WingPressure");growl=Resources.Load<AudioClip>("LordLyricalAccents/DragonHarmonicGrowl");word=Resources.Load<AudioClip>("LordLyricalAccents/WordChorus");
            for(int i=0;i<pool.Length;i++){var g=new GameObject("Spatial accent "+i);g.transform.SetParent(transform,false);var s=g.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=1;s.minDistance=3;s.maxDistance=30;s.rolloffMode=AudioRolloffMode.Linear;s.priority=180;pool[i]=s;}
        }
        public void Cue(Cue cue){
            AudioClip clip=cue.id switch{"landing"=>foot,"orc-emerge"=>fork,"orc-stomp"=>fork,"resonance-shatter"=>glass,"dragon-unfold"=>wing,"dragon-roar"=>growl,"the-word"=>word,"anti-frequency"=>glass,_=>null};
            if(!clip)return;var s=pool[next++%pool.Length];s.Stop();s.clip=clip;s.volume=mix;s.transform.position=cue.authoredTime>=63?new Vector3(0,7,11):new Vector3(0,0,12);s.PlayScheduled(AudioSettings.dspTime+.01);
        }
        public void Stop(){foreach(var s in pool)if(s)s.Stop();}
        void OnDisable(){Stop();}
    }
}
''')
(root/'Assets/LordLyrical/Scripts/XR/HapticsDirector.cs').write_text('''using UnityEngine;
using UnityEngine.XR;
namespace LordLyrical
{
    public class HapticsDirector:MonoBehaviour
    {
        [Range(0,1)]public float intensity=.65f;
        struct Beat{public float offset,strength,duration;public Beat(float t,float a,float d){offset=t;strength=a;duration=d;}}
        Beat[] beats;int next;float began;
        public void Cue(Cue cue,int slot){
            int role=cue.id switch{"seeker-map"=>6,"deep-sonar"=>6,"resonance-shatter"=>2,"infrasound-dread"=>5,"healer-maximum"=>4,"anti-frequency"=>8,_=>-1};
            if(role>=0&&slot!=role)return;
            beats=cue.id switch{
                "the-word"=>new[]{new Beat(0,.7f,.12f),new Beat(.16f,.48f,.12f),new Beat(.34f,.26f,.18f)},
                "ten-key-lock"=>new[]{new Beat(0,.25f,.06f),new Beat(.22f,.25f,.06f)},
                "amplitude-rise"=>new[]{new Beat(0,.12f,.08f),new Beat(.45f,.2f,.10f),new Beat(.9f,.32f,.12f),new Beat(1.4f,.4f,.14f)},
                "seeker-map" or "deep-sonar"=>new[]{new Beat(0,.15f,.035f),new Beat(.18f,.1f,.035f),new Beat(.36f,.07f,.035f)},
                _=>cue.category=="Impact"?new[]{new Beat(0,.32f,.13f)}:null};next=0;began=Time.unscaledTime;Tick();
        }
        void Update(){Tick();}
        void Tick(){if(beats==null)return;float age=Time.unscaledTime-began;while(next<beats.Length&&age>=beats[next].offset){var b=beats[next++];Pulse(b.strength,b.duration);}if(next>=beats.Length)beats=null;}
        public void Pulse(float strength,float seconds){Send(XRNode.LeftHand,strength*intensity,seconds);Send(XRNode.RightHand,strength*intensity,seconds);}
        static void Send(XRNode node,float strength,float seconds){var d=InputDevices.GetDeviceAtXRNode(node);if(d.TryGetHapticCapabilities(out var c)&&c.supportsImpulse)d.SendHapticImpulse(0,Mathf.Clamp01(strength),Mathf.Clamp(seconds,0,.5f));}
        public void Stop(){beats=null;InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).StopHaptics();InputDevices.GetDeviceAtXRNode(XRNode.RightHand).StopHaptics();}
        void OnDisable(){Stop();}
    }
}
''')
for f in (out/'LordLyricalFinishing_Unity').rglob('*'):
 if f.is_file():
  dest=root/'Assets/LordLyricalFinishing_Unity'/f.relative_to(out/'LordLyricalFinishing_Unity');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,dest)
print('RUNTIME_DETAIL_AUDIO_READY')
