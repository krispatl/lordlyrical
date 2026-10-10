using UnityEngine;
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
            AudioClip clip=cue.id switch{"landing"=>foot,"orc-emerge"=>fork,"orc-stomp"=>fork,"resonance-shatter"=>glass,"wings-open"=>wing,"dragon-roar"=>growl,"the-word"=>word,"anti-frequency"=>glass,_=>null};
            if(!clip)return;var s=pool[next++%pool.Length];s.Stop();s.clip=clip;s.volume=mix;s.transform.position=cue.authoredTime>=63?new Vector3(0,7,11):new Vector3(0,0,12);s.PlayScheduled(AudioSettings.dspTime+.01);
        }
        public void Stop(){foreach(var s in pool)if(s)s.Stop();}
        void OnDisable(){Stop();}
    }
}
