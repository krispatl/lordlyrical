using UnityEngine;
namespace LordLyrical
{
    // Quiet diegetic accents; the supplied music remains the mix and timing authority.
    public sealed class SpatialAccents:MonoBehaviour
    {
        readonly AudioSource[] pool=new AudioSource[4];AudioClip impact;int next;
        void Awake()
        {
            const int rate=22050;var samples=new float[rate/2];
            for(int i=0;i<samples.Length;i++){float t=i/(float)rate;float attack=Mathf.Min(1,t*100);samples[i]=attack*Mathf.Exp(-t*12)*(Mathf.Sin(t*2*Mathf.PI*(84-35*t))*.6f+Mathf.Sin(t*2*Mathf.PI*173)*.12f);}
            impact=AudioClip.Create("Ritual resonance impact",samples.Length,1,rate,false);impact.SetData(samples,0);
            for(int i=0;i<pool.Length;i++){var g=new GameObject("Spatial resonance voice "+i);g.transform.SetParent(transform);var s=g.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=1;s.minDistance=3;s.maxDistance=30;s.rolloffMode=AudioRolloffMode.Linear;s.volume=.16f;s.clip=impact;pool[i]=s;}
        }
        public void Cue(Cue cue){if(cue.category!="Impact")return;var s=pool[next++%pool.Length];s.Stop();s.transform.position=cue.time>=63?new Vector3(0,7,11):new Vector3(0,0,8);s.PlayScheduled(AudioSettings.dspTime+.01);}
        public void Stop(){foreach(var s in pool)if(s!=null)s.Stop();}
        void OnDestroy(){if(impact!=null)Destroy(impact);}
    }
}
