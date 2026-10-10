using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    public sealed class OrcAttackVisual:MonoBehaviour
    {
        LineRenderer telegraph,trail;Material material;readonly Vector3[] arc=new Vector3[35],history=new Vector3[8];
        int previousSerial=-1;float previousTime=-1;
        void Ensure()
        {
            if(telegraph)return;
            material=new Material(Resources.Load<Shader>("Concert/ScoreLight"));
            LineRenderer Line(string name,int count,float width){
                var g=new GameObject(name);g.transform.SetParent(transform,false);var l=g.AddComponent<LineRenderer>();
                l.sharedMaterial=material;l.positionCount=count;l.widthMultiplier=width;l.useWorldSpace=true;
                l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;l.numCapVertices=2;return l;
            }
            telegraph=Line("Attack resonance footprint",35,.025f);trail=Line("Weapon motion ribbon",8,.045f);
        }
        public void Sample(ConcertOrc o,float time,Transform socket)
        {
            Ensure();bool attacking=o.intent==OrcIntent.Telegraph||o.intent==OrcIntent.Strike;
            telegraph.enabled=attacking;trail.enabled=o.intent==OrcIntent.Strike&&socket;
            float age=Mathf.Max(0,time-o.attackStarted);
            Color c=o.intent==OrcIntent.Telegraph?new Color(1,.24f,.035f):new Color(1,.72f,.2f);
            telegraph.startColor=telegraph.endColor=c*Combat34Score.OrcWarning(o,time)*1.25f;
            if(attacking){
                float half=o.attack==OrcAttack.Sweep||o.attack==OrcAttack.Double?65:o.attack==OrcAttack.Burst?48:32;
                float radius=OrcCombatMotion.ContactRange(o.attack);var origin=o.position+Vector3.up*.06f;
                arc[0]=origin;arc[34]=origin;var rotation=Quaternion.Euler(0,o.yaw,0);
                for(int i=1;i<34;i++){float angle=Mathf.Lerp(-half,half,(i-1)/32f)*Mathf.Deg2Rad;arc[i]=origin+rotation*new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);}
                telegraph.SetPositions(arc);
            }
            if(socket){
                Vector3 tip=socket.TransformPoint(Vector3.up*1.1f);
                if(previousSerial!=o.attackSerial||time<previousTime||time-previousTime>.25f)for(int i=0;i<8;i++)history[i]=tip;
                else for(int i=7;i>0;i--)history[i]=history[i-1];
                history[0]=tip;trail.SetPositions(history);trail.startColor=new Color(1,.65f,.15f);trail.endColor=new Color(1,.12f,.015f,0);
            }
            previousSerial=o.attackSerial;previousTime=time;
        }
        void OnDestroy(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
}
