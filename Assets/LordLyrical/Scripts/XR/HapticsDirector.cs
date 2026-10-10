using UnityEngine;
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
        // Impacts use the existing sound and visual cues in the hands-only experience.
        public void Pulse(float strength,float seconds){}
        public void Stop(){beats=null;}
        void OnDisable(){Stop();}
    }
}
