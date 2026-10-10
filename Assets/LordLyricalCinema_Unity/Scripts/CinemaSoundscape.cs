using UnityEngine;
namespace LordLyrical
{
    public sealed class CinemaSoundscape : MonoBehaviour
    {
        AudioSource opening,returning,titleDissolve;
        void Awake(){opening=Source("Room resonance","Cinema26/RoomAwakening",.12f);returning=Source("Realm release","Cinema26/RealmRelease",.16f);titleDissolve=Source("Title shimmer static","Cinema26/TitleDissolve30",.22f);titleDissolve.spatialBlend=0;}
        AudioSource Source(string title,string resource,float volume){var go=new GameObject(title);go.transform.SetParent(transform,false);go.transform.position=new Vector3(0,1.5f,5);var s=go.AddComponent<AudioSource>();s.clip=Resources.Load<AudioClip>(resource);s.playOnAwake=false;s.loop=false;s.volume=volume;s.spatialBlend=.75f;s.minDistance=3;s.maxDistance=18;s.dopplerLevel=0;s.rolloffMode=AudioRolloffMode.Linear;s.priority=110;return s;}
        static void Sample(AudioSource s,float age){if(!s||!s.clip)return;if(age<0||age>=s.clip.length){if(s.isPlaying)s.Stop();return;}if(!s.isPlaying){s.time=age;s.Play();}else if(Mathf.Abs(s.time-age)>.15f)s.time=age;}
        public void Render(float t,bool waiting){if(!Application.isPlaying)return;if(waiting){if(opening)opening.Stop();if(returning)returning.Stop();if(titleDissolve)titleDissolve.Stop();return;}Sample(opening,t+.7f);Sample(returning,t-CinemaTiming.ReturnStart);Sample(titleDissolve,t-(t<0?CinemaTiming.IntroTitleFadeStart:CinemaTiming.EndTitleFadeStart));}
        void OnDisable(){if(opening)opening.Stop();if(returning)returning.Stop();if(titleDissolve)titleDissolve.Stop();}
    }
}