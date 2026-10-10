using System;
using UnityEngine;
using CrazyMinnow.SALSA;
using LordLyrical.Characters;

namespace LordLyrical
{
    [Serializable] public class VocalWindow { public float start,end,confidence; }
    [Serializable] public class VocalWindows { public VocalWindow[] windows; }
    [DefaultExecutionOrder(300)]
    public sealed class JeruConcertFace : MonoBehaviour
    {
        public Salsa salsa;
        JeruPerformance performance; FacialLodRelay relay; Animator rig;
        static VocalWindows track;
        float songTime,voice; bool running;
        public float VocalActivity=>voice;
        public static float Activity(float t)
        {
            if(track==null){var a=Resources.Load<TextAsset>("Concert/VocalWindows");track=a?JsonUtility.FromJson<VocalWindows>(a.text):new VocalWindows{windows=Array.Empty<VocalWindow>()};}
            var ws=track.windows;int lo=0,hi=ws.Length;
            while(lo<hi){int m=(lo+hi)/2;if(ws[m].start<t+.055f)lo=m+1;else hi=m;}
            float v=0;for(int i=Math.Max(0,lo-3);i<Math.Min(ws.Length,lo+1);i++){
                var w=ws[i];float on=Mathf.SmoothStep(0,1,Mathf.InverseLerp(w.start-.045f,w.start+.035f,t));
                float off=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(w.end-.02f,w.end+.065f,t));v=Mathf.Max(v,on*off*Mathf.Lerp(.65f,1,w.confidence));
            }return v;
        }
        void Awake(){performance=GetComponent<JeruPerformance>();relay=GetComponent<FacialLodRelay>();rig=GetComponentInChildren<Animator>();if(!salsa)salsa=GetComponent<Salsa>();if(salsa){salsa.useExternalAnalysis=true;salsa.getExternalAnalysis=Analysis;}}
        float Analysis()=>running?voice:0;
        public void Sample(float t,bool live)
        {
            songTime=t;running=live;var m=MusicReactiveSystem.Sample(t);
            // Word windows only suppress instrumental passages. This is amplitude-based lip sync, not phoneme alignment.
            voice=live?Activity(t)*Mathf.Clamp01(.1f+m.mid*.63f+m.air*.18f):0;
            if(!performance)performance=GetComponent<JeruPerformance>();
            if(!Application.isPlaying&&performance){performance.SetExpression("viseme_aa",voice*.56f);performance.SetExpression("viseme_E",voice*.20f);}
        }
        void LateUpdate()
        {
            if(!performance)return;float t=songTime;
            float blink=Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(Mathf.Repeat(t+1.37f,3.71f)-.13f)/.13f),2);
            performance.SetExpression("eyeBlinkLeft",blink);performance.SetExpression("eyeBlinkRight",blink);
            performance.SetExpression("browInnerUp",.07f+voice*.17f);
            performance.SetExpression("browDownLeft",t>=26&&t<54?voice*.18f:0);
            performance.SetExpression("browDownRight",t>=26&&t<54?voice*.14f:0);
            performance.SetExpression("mouthSmileLeft",t>=101&&t<111?.15f:0);
            performance.SetExpression("mouthSmileRight",t>=101&&t<111?.15f:0);
            if(rig&&rig.isHuman){var head=rig.GetBoneTransform(HumanBodyBones.Head);var cam=Camera.main;
                if(head&&cam){Vector3 local=transform.InverseTransformDirection(cam.transform.position-head.position);float yaw=Mathf.Clamp(Mathf.Atan2(local.x,-local.z)*Mathf.Rad2Deg,-18,18);
                    head.rotation=Quaternion.AngleAxis(yaw*.3f,Vector3.up)*head.rotation;}
            }
            if(relay)relay.Synchronize();
        }
    }
}
