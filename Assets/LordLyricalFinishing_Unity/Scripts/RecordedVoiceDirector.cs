using UnityEngine;
using LordLyrical.Characters;
namespace LordLyrical
{
    // Empty slots are silent. Assigned clips reconstruct at their current position on a seek/join.
    public sealed class RecordedVoiceDirector:MonoBehaviour
    {
        VoiceCueLibrary library;Cue[] cues;ResonantWorld world;AudioSource source;VoiceCueLibrary.Entry active;JeruPerformance jeru;
        public void Initialize(VoiceCueLibrary data,Cue[] table,ResonantWorld realm)
        {library=data;cues=table;world=realm;var speaker=new GameObject("Spatial recorded voice");speaker.transform.SetParent(transform,false);source=speaker.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.minDistance=3;source.maxDistance=24;source.rolloffMode=AudioRolloffMode.Linear;source.priority=16;jeru=world.GetComponentInChildren<JeruPerformance>(true);}
        void ClearFace(){if(active?.facialPerformance&&active.slot<0&&jeru)foreach(var c in active.facialPerformance.channels)jeru.SetExpression(c.blendShape,0);}
        public void Tick(double musicTime)
        {
            VoiceCueLibrary.Entry selected=null;float age=0;
            if(library)foreach(var e in library.entries){if(!e.recording)continue;foreach(var c in cues)if(c.id==e.cueId&&musicTime>=c.time&&musicTime<c.time+e.recording.length){selected=e;age=(float)(musicTime-c.time);break;}}
            if(selected==null){Stop();return;}
            if(active!=selected){ClearFace();source.Stop();active=selected;source.clip=active.recording;source.volume=active.gain;source.timeSamples=Mathf.Clamp((int)(age*source.clip.frequency),0,source.clip.samples-1);source.Play();}
            else if(Mathf.Abs(source.time-age)>.15f)source.timeSamples=Mathf.Clamp((int)(age*source.clip.frequency),0,source.clip.samples-1);
            source.transform.position=world.VoicePosition(active.slot);
            if(active.facialPerformance&&active.slot<0&&jeru)foreach(var c in active.facialPerformance.channels)jeru.SetExpression(c.blendShape,c.weight.Evaluate(age));
        }
        public void Stop(){if(source)source.Stop();ClearFace();active=null;}
        void OnDisable(){Stop();}
    }
}
