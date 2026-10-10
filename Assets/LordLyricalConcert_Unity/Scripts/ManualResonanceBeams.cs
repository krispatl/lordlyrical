using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using Styly.NetSync;

namespace LordLyrical
{
    [Serializable] public sealed class PalmBeamPacket
    {
        public string epoch;public int run,slot,sequence;public float time;
        public bool left,right;public Vector3 leftOrigin,rightOrigin,leftDirection,rightDirection;
    }
    public sealed class ManualResonanceBeams : MonoBehaviour
    {
        sealed class Beam {public LineRenderer core,glow;public Vector3 origin,direction,targetOrigin,targetDirection;public bool active;public float received;public int owner;public string epoch;}
        readonly Beam[] beams=new Beam[20];
        readonly float[] held=new float[2],charge=new float[2];
        readonly Vector3[] chargeOrigin=new Vector3[2],chargeDirection=new Vector3[2];
        readonly ParticipantInputSequence sequences=new();
        readonly Vector3[] points=new Vector3[16];
        NetworkSessionController session;ConcertEncounter encounter;NetSyncManager net;TrackedRig rig;
        Material filament;MagicalPalmVFX magic;int serial,run=-1;float clock,lastSent;int previousSlot=-1;bool wasFiring,live;
        Action<int,Vector3,Vector3> fire;readonly float[] damageAt=new float[2];readonly float[] impactAt=new float[20];readonly int[] seenHits=new int[12];CombatImpactVFX impactFX;
        public int LocalSlot=>session?session.Slot:-1;
        public int ActiveBeams {get;private set;}
        public bool TryRenderedRay(int slot,int side,out Vector3 origin,out Vector3 direction){
            origin=direction=default;if(slot<0||slot>=10||side<0||side>1)return false;
            var b=beams[slot*2+side];if(b==null||!b.core||!b.core.enabled||!live)return false;origin=b.origin;direction=b.direction;return true;
        }
        public MagicalPalmVFX Presentation=>magic;
        public bool HandsAvailable=>rig&&rig.Hands&&rig.Hands.Available;
        public static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        public static bool ValidRay(Vector3 origin,Vector3 direction,Vector3 head)
            =>Finite(origin)&&Finite(direction)&&Vector3.Distance(origin,head)<1.8f&&direction.sqrMagnitude>.9f&&direction.sqrMagnitude<1.1f;
        public void Initialize(NetworkSessionController network,ConcertEncounter combat,Action<int,Vector3,Vector3> onFire)
        {
            session=network;encounter=combat;fire=onFire;
            filament=new Material(Shader.Find("LordLyrical/Resonant Filament"));filament.SetFloat("_CoreWhite",.35f);filament.SetColor("_Color",Color.white);
            for(int i=0;i<beams.Length;i++){
                var b=beams[i]=new Beam();
                b.core=Line("Palm beam core "+i,.014f);b.glow=Line("Palm beam glow "+i,.078f);
            }
            var director=FindFirstObjectByType<ExperienceDirector>();
            var source=director&&director.configuration.finishingModel?director.configuration.finishingModel.GetComponent<CinematicParticleDirector>():null;
            impactFX=gameObject.AddComponent<CombatImpactVFX>();impactFX.Initialize(source);
            magic=gameObject.AddComponent<MagicalPalmVFX>();magic.Initialize(source,filament);
        }
        LineRenderer Line(string name,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var l=go.AddComponent<LineRenderer>();
            l.sharedMaterial=filament;l.positionCount=16;l.widthMultiplier=width;l.useWorldSpace=true;l.enabled=false;
            l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;l.numCapVertices=2;
            l.widthCurve=new AnimationCurve(new Keyframe(0,.40f),new Keyframe(.05f,1),new Keyframe(.85f,.88f),new Keyframe(1,.35f));return l;
        }
        public static bool RaySphere(Vector3 origin,Vector3 direction,Vector3 center,float radius,out float distance)
        {
            Vector3 v=origin-center;float b=Vector3.Dot(v,direction),c=v.sqrMagnitude-radius*radius,d=b*b-c;
            distance=0;if(d<0)return false;float a=-b-Mathf.Sqrt(d),z=-b+Mathf.Sqrt(d);
            if(z<0)return false;distance=Mathf.Max(0,a);return true;
        }
        public static int Target(ConcertState state,Vector3 origin,Vector3 direction,float range,out float distance)
        {
            return BeamHitResolver.Target(state,origin,direction,range,out distance,out _);
        }
        bool Input(int side,out Vector3 origin,out Vector3 direction,out bool trackedHand)
        {
            origin=direction=default;trackedHand=false;if(!rig)return false;
            var h=rig.Hands?(side==0?rig.Hands.Left:rig.Hands.Right):default;
            if(h.tracked){origin=h.position;direction=h.direction;trackedHand=true;return h.open;}
            // Loss of hand tracking never falls back to a controller pose or trigger.
            return false;
        }
        public void Render(float t,bool showing,float songTime=float.NaN)
        {
            clock=t;float music=float.IsNaN(songTime)?t:songTime;live=showing&&Application.isPlaying&&Application.isFocused&&PalmBeamTiming.Allowed(music)&&session.Slot>=0&&session.Ready;
            if(!rig)rig=FindFirstObjectByType<TrackedRig>();
            if(!net&&session.Manager){net=session.Manager;net.OnRPCReceived.AddListener(Rpc);}
            int current=session.Snapshot?.runId??0;
            if(run!=current||!live){foreach(var b in beams)b.active=false;Array.Clear(held,0,2);run=current;Array.Clear(seenHits,0,seenHits.Length);if(impactFX)impactFX.Clear();if(magic)magic.Clear();}
            var packet=new PalmBeamPacket{epoch=session.InputEpoch,run=run,slot=session.Slot,time=t,sequence=++serial};
            if(previousSlot!=session.Slot){StopSlot(previousSlot);previousSlot=session.Slot;}
            bool firing=false;
            if(!live||!Application.isFocused)StopSlot(session.Slot);
            if(live&&Application.isFocused){
                for(int side=0;side<2;side++){
                    bool intent=Input(side,out var origin,out var direction,out bool hand);
                    held[side]=intent?held[side]+Time.unscaledDeltaTime:0;
                    bool on=intent&&held[side]>=(hand?.18f:0);
                    charge[side]=intent&&hand&&!on?Mathf.Clamp01(held[side]/.18f):0;chargeOrigin[side]=origin;chargeDirection[side]=direction;
                    var b=beams[session.Slot*2+side];
                    // Tracking loss or a closed hand cancels immediately; only acquisition has a dwell.
                    b.active=on;b.received=Time.unscaledTime;
                    b.origin=origin;
                    b.direction=b.direction.sqrMagnitude>.1f&&on?Vector3.Slerp(b.direction,direction,1-Mathf.Exp(-Time.unscaledDeltaTime*24)).normalized:direction;
                    if(side==0){packet.left=on;packet.leftOrigin=b.origin;packet.leftDirection=b.direction;}
                    else {packet.right=on;packet.rightOrigin=b.origin;packet.rightDirection=b.direction;}
                    firing|=on;
                    
                }
            }
            if(live&&firing)for(int side=0;side<2;side++){
                var b=beams[session.Slot*2+side];
                if(b.active&&Time.unscaledTime-damageAt[side]>=.10f){damageAt[side]=Time.unscaledTime;fire?.Invoke(side,b.origin,b.direction);}
            }
            if(net&&net.IsReady&&session.Slot>=0&&(firing!=wasFiring||live&&Time.unscaledTime-lastSent>.10f)){
                lastSent=Time.unscaledTime;net.Rpc("ll.concert.palms",new[]{JsonUtility.ToJson(packet)});
            }
            wasFiring=firing;
            Draw(t,Time.unscaledDeltaTime,Time.unscaledTime);
        }
        void Draw(float t,float dt,float now)
        {
            ActiveBeams=0;
            Vector3 viewer=rig&&rig.head?rig.head.position:Camera.main?Camera.main.transform.position:Vector3.zero;
            magic.Begin(t,now,viewer,live);
            if(live&&session.Slot>=0)for(int side=0;side<2;side++)if(charge[side]>0)
                magic.Charge(session.Slot*2+side,chargeOrigin[side],chargeDirection[side],RootKeys.Slots[session.Slot].color,charge[side]);
            for(int i=0;i<beams.Length;i++){
                var b=beams[i];bool local=i/2==session.Slot;
                if(!local&&b.active&&
                   (ParticipantInputSequence.OwnerOf(session.Participants,i/2)!=b.owner||
                    !net||net.GetClientVariable(NetworkSessionController.InputEpochKey,b.owner)!=b.epoch))b.active=false;
                bool on=live&&b.active&&now-b.received<.35f;
                b.core.enabled=b.glow.enabled=on;if(!on)continue;ActiveBeams++;
                if(!local){
                    float blend=1-Mathf.Exp(-Mathf.Max(0,dt)*30);
                    b.origin=Vector3.Lerp(b.origin,b.targetOrigin,blend);
                    b.direction=Vector3.Slerp(b.direction,b.targetDirection,blend).normalized;
                }
                var contact=BeamHitResolver.Trace(encounter.State,t,b.origin,b.direction);
                float range=contact.distance;
                if(t>=75&&t<81||t>=92&&t<100){
                    var target=t<82?encounter.State.shieldPoint:encounter.State.dragonHeart;
                    if(RaySphere(b.origin,b.direction,target,t<82?1.35f:1.7f,out float hit)&&hit<range){
                        range=hit;contact.hit=true;contact.point=b.origin+b.direction*range;contact.normal=(contact.point-target).normalized;contact.orc=-1;
                    }
                }
                bool enhanced=Counterattack38.Enhanced(encounter.State.counters[i/2],t,i%2);
                Vector3 end=b.origin+b.direction*range;Color color=RootKeys.Slots[i/2].color;
                if(impactFX&&contact.hit&&now-impactAt[i]>=.10f){
                    impactAt[i]=now;impactFX.Hit(end,contact.normal,enhanced?Color.Lerp(color,Color.white,.45f):color,contact.orc>=0,false,true);
                }
                for(int j=0;j<points.Length;j++){float u=j/(float)(points.Length-1);points[j]=Vector3.Lerp(b.origin,end,u);}
                b.core.SetPositions(points);b.glow.SetPositions(points);
                float pulse=1+.08f*Mathf.Sin(t*8.7f+i*.73f)+.035f*Mathf.Sin(t*17+i);
                b.core.widthMultiplier=.014f*pulse*(enhanced?2.1f:1);b.glow.widthMultiplier=.078f*pulse*(enhanced?1.8f:1);
                b.core.startColor=b.core.endColor=Color.Lerp(color,Color.white,.72f);
                var soft=color;soft.a=enhanced?.27f:.14f;b.glow.startColor=b.glow.endColor=soft;
                magic.Beam(i,b.origin,end,contact.normal,contact.hit,color,local);
            }
            if(impactFX&&encounter.State?.orcs!=null)for(int i=0;i<12;i++){
                var o=encounter.State.orcs[i];
                if(o.hitSerial>seenHits[i]){
                    seenHits[i]=o.hitSerial;
                    if(t-o.lastHitTime<.45f)impactFX.Hit(o.lastHitPoint,o.lastHitNormal,RootKeys.Slots[Mathf.Clamp(o.lastHitSlot,0,9)].color,true,o.health<=0);
                }
            }
            magic.End();
        }
        void Rpc(int sender,string method,string[] args)
        {
            if(net&&sender!=net.ClientNo&&net.GetClientVariable("ll.concert",sender)!="10")return;
            if(method!="ll.concert.palms"||!live||args==null||args.Length!=1||args[0].Length>1800)return;
            try{
                var p=JsonUtility.FromJson<PalmBeamPacket>(args[0]);if(p==null||p.run!=run||p.slot<0||p.slot>=10||p.slot==session.Slot||!float.IsFinite(p.time)||Mathf.Abs(p.time-clock)>.75f)return;
                int owner=ParticipantInputSequence.OwnerOf(session.Participants,p.slot);
                if(owner!=sender||!NetworkRoleAvatar.TryGetHead(p.slot,out var head))return;
                if(p.left&&!ValidRay(p.leftOrigin,p.leftDirection,head)||p.right&&!ValidRay(p.rightOrigin,p.rightDirection,head))return;
                if(!sequences.TryAccept(run,p.run,p.slot,sender,owner,p.epoch,net.GetClientVariable(NetworkSessionController.InputEpochKey,sender),p.sequence))return;
                for(int side=0;side<2;side++){
                    var b=beams[p.slot*2+side];
                    b.targetOrigin=side==0?p.leftOrigin:p.rightOrigin;
                    b.targetDirection=side==0?p.leftDirection:p.rightDirection;
                    if(!b.active||b.owner!=sender||b.epoch!=p.epoch){b.origin=b.targetOrigin;b.direction=b.targetDirection;}
                    b.active=side==0?p.left:p.right;b.owner=sender;b.epoch=p.epoch;b.received=Time.unscaledTime;
                }
            }catch(ArgumentException){}
        }
        void StopSlot(int slot)
        {
            Array.Clear(held,0,held.Length);Array.Clear(charge,0,charge.Length);
            if(slot<0||slot>=10)return;
            for(int side=0;side<2;side++){var b=beams[slot*2+side];if(b!=null){b.active=false;b.core.enabled=b.glow.enabled=false;}}
        }
        void OnApplicationFocus(bool focused){if(!focused){StopSlot(session?session.Slot:-1);foreach(var b in beams)if(b!=null){b.active=false;b.core.enabled=b.glow.enabled=false;}ActiveBeams=0;if(magic)magic.Clear();if(impactFX)impactFX.Clear();}}
        void OnApplicationPause(bool paused){if(paused)OnApplicationFocus(false);}
        void OnDisable(){StopSlot(session?session.Slot:-1);foreach(var b in beams)if(b!=null){b.active=false;b.core.enabled=b.glow.enabled=false;}if(magic)magic.Clear();if(impactFX)impactFX.Clear();}
        void OnDestroy(){if(net)net.OnRPCReceived.RemoveListener(Rpc);if(filament){if(Application.isPlaying)Destroy(filament);else DestroyImmediate(filament);}}
    }
}
