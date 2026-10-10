using UnityEngine;
namespace LordLyrical
{
    public sealed class ArrivalSoundscape : MonoBehaviour
    {
        AudioSource charge,strike,thunder;
        void Awake(){
            charge=Source("Lightning descent","LordLyricalAccents/LightningDescent",.34f,new Vector3(0,5,5));
            strike=Source("Lightning landing","LordLyricalAccents/LightningLanding",.56f,new Vector3(0,.7f,5));
            thunder=Source("Arrival thunder","LordLyricalAccents/ArrivalThunder",.27f,new Vector3(0,4,6));
        }
        AudioSource Source(string name,string path,float gain,Vector3 position){
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=position;
            var s=go.AddComponent<AudioSource>();s.clip=Resources.Load<AudioClip>(path);s.playOnAwake=false;s.loop=false;
            s.volume=gain;s.spatialBlend=.85f;s.minDistance=3;s.maxDistance=24;s.rolloffMode=AudioRolloffMode.Linear;s.dopplerLevel=0;s.priority=60;return s;
        }
        static void Sample(AudioSource source,float age){
            if(!source||!source.clip)return;
            if(age<0||age>=source.clip.length){if(source.isPlaying)source.Stop();return;}
            if(!source.isPlaying){source.time=age;source.Play();}
            else if(Mathf.Abs(source.time-age)>.12f)source.time=age;
        }
        public void Render(float time,bool waiting){
            if(!Application.isPlaying||waiting){Stop();return;}
            Sample(charge,time-OpeningTiming.Arrival);
            Sample(strike,time-OpeningTiming.Landing);
            Sample(thunder,time-(OpeningTiming.Landing+.09f));
        }
        public void Stop(){if(charge)charge.Stop();if(strike)strike.Stop();if(thunder)thunder.Stop();}
        void OnDisable(){Stop();}
    }
}
