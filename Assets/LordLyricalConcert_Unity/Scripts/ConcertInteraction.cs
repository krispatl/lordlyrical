using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Styly.NetSync;

namespace LordLyrical
{
    [Serializable] public class ConcertAction {public string epoch;public int run,slot,sequence,hand;public float time;public bool guard,beam;public Vector3 origin,direction;}
    [Serializable] public class ConcertContact {public int run,slot,sequence;public float time;public bool blocked;}
    public sealed class ConcertInteraction : MonoBehaviour
    {
        public ConcertEncounter Encounter {get;private set;}
        NetworkSessionController session;NetSyncManager net;OrcEncounterController actors;
        float last=-1,now,lastSend,lastInput,lastGuard,feedbackUntil,songNow;int run=-1,actionSequence,snapshotSequence,lastSnapshot=-1,host=-1,advertisedFor=-1;
        int learnedGestures;GestureDemonstration40 demonstration;
        readonly ParticipantInputSequence inputSequences=new();bool pressed;bool active;
        int contactSequence,lastContactSequence;string advertisedEpoch;
        SpatialInstructionCard cueCard;GuardContactPresentation guardContact;SharedFistShields37 fistShields;
        LineRenderer pulseRing;TextMesh feedback;Material ringMaterial;Vector3 pulseOrigin;float pulseAt=-100;Color pulseColor;
        Vector3 shieldTarget=new Vector3(0,1.8f,5.7f),heartTarget=new Vector3(0,8,11);
        public void SetStageTargets(Vector3 shield,Vector3 heart,Vector3 mouth){shieldTarget=shield;heartTarget=heart;Encounter.DragonMouth=mouth;}
        Crescendo38 crescendo;NavMeshDataInstance nav;ManualResonanceBeams manualBeams;TrackedRig inputRig;
        public void Initialize(NetworkSessionController network,OrcEncounterController orcs){session=network;actors=orcs;Encounter=new ConcertEncounter();Encounter.Impact+=Impact;
            manualBeams=gameObject.AddComponent<ManualResonanceBeams>();manualBeams.Initialize(session,Encounter,(hand,origin,direction)=>Submit(false,true,origin,direction,hand));
            var data=Resources.Load<NavMeshData>("Concert/ChamberNavigation");if(data)nav=NavMesh.AddNavMeshData(data);
            var ring=new GameObject("Your harmonic pulse");ring.transform.SetParent(transform,false);pulseRing=ring.AddComponent<LineRenderer>();pulseRing.loop=true;pulseRing.positionCount=64;pulseRing.widthMultiplier=.025f;ringMaterial=new Material(Resources.Load<Shader>("Concert/ScoreLight"));pulseRing.sharedMaterial=ringMaterial;
            var label=new GameObject("Concert interaction feedback");label.transform.SetParent(transform,false);feedback=label.AddComponent<TextMesh>();feedback.anchor=TextAnchor.MiddleCenter;feedback.alignment=TextAlignment.Center;feedback.fontSize=52;feedback.characterSize=.014f;
            cueCard=label.AddComponent<SpatialInstructionCard>();cueCard.Initialize(feedback);demonstration=label.AddComponent<GestureDemonstration40>();demonstration.Initialize();label.transform.localScale=Vector3.one*1.5f;
            guardContact=new GameObject("Player guard confirmation").AddComponent<GuardContactPresentation>();guardContact.transform.SetParent(transform,false);guardContact.Initialize();fistShields=new GameObject("Avatar coloured fist shields").AddComponent<SharedFistShields37>();fistShields.transform.SetParent(transform,false);fistShields.Initialize(session,Encounter);crescendo=gameObject.AddComponent<Crescendo38>();crescendo.Initialize(Encounter);
        }
        public static bool IsClockJump(float delta)=>delta<-.25f||delta>.5f;
        int ElectedHost(){int chosen=net?net.ClientNo:-1;if(net)foreach(var p in session.Participants.participants)if(p.connected&&net.GetClientVariable("ll.concert",p.clientNo)=="10")chosen=Math.Min(chosen,p.clientNo);return chosen;}
        bool Authority=>!net||!net.IsReady||net.ClientNo==host;
        public void Render(float song,float narrative,bool showing)
        {
            now=narrative;songNow=song;active=showing&&Application.isPlaying;int currentRun=session.Snapshot?.runId??0;
            if(net==null&&session.Manager){net=session.Manager;net.OnRPCReceived.AddListener(Rpc);}
            if(net&&net.IsReady&&(advertisedFor!=net.ClientNo||advertisedEpoch!=session.InputEpoch)&&net.SetClientVariable("ll.concert","10")){advertisedFor=net.ClientNo;advertisedEpoch=session.InputEpoch;}
            int elected=ElectedHost();if(elected!=host){host=elected;lastSnapshot=-1;lastContactSequence=0;snapshotSequence=Math.Max(snapshotSequence,Encounter.State.sequence);}
            float dt=last<0?0:narrative-last;
            if(run!=currentRun||IsClockJump(dt)||!showing){run=currentRun;Encounter.Reset(run,narrative);lastSnapshot=-1;lastContactSequence=0;pressed=false;learnedGestures=0;feedbackUntil=0;pulseAt=-100;if(guardContact)guardContact.Clear();}
            last=narrative;Encounter.SetDuelTargets(shieldTarget,heartTarget);
            for(int i=0;i<10;i++){Encounter.occupied[i]=false;Encounter.heads[i]=RootKeys.Slots[i].spawn+Vector3.up*1.65f;}
            foreach(var p in session.Participants.participants)if(p.connected&&p.slot>=0&&p.slot<10&&(!net||p.clientNo==net.ClientNo||net.GetClientVariable("ll.concert",p.clientNo)=="10")){if(NetworkRoleAvatar.TryGetHead(p.slot,out var head)){Encounter.occupied[p.slot]=true;Encounter.heads[p.slot]=head;}}
            if(session.Slot>=0&&Camera.main){Encounter.occupied[session.Slot]=true;Encounter.heads[session.Slot]=Camera.main.transform.position;}
            fistShields.Render(narrative,active&&Application.isFocused);
            if(active){if(session.Ready&&Application.isFocused)ReadInput(song);if(Authority){Encounter.Tick(narrative,Mathf.Clamp(dt,0,.05f));if(net&&net.IsReady&&Time.unscaledTime-lastSend>.15f){lastSend=Time.unscaledTime;Encounter.State.sequence=++snapshotSequence;net.Rpc("ll.concert.state",new[]{JsonUtility.ToJson(Encounter.State)});}}actors.RenderInteractive(Encounter.State,narrative,Encounter.heads);}
            if(!inputRig)inputRig=FindFirstObjectByType<TrackedRig>();
            
            crescendo.Render(narrative,active);manualBeams.Render(narrative,active,song);Shader.SetGlobalFloat("_ConcertHarmony",Encounter.State.harmony);DrawFeedback(song,showing);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(active&&Array.IndexOf(Environment.GetCommandLineArgs(),"--concert-smoke-test")>=0&&song>27&&song<50){
                if((song<29||song>46)&&song-lastTestPulse>.52f){lastTestPulse=song;Submit(false);}
                if(song-lastTestGuard>.18f){lastTestGuard=song;Submit(true);}
            }
#endif
        }
        float lastTestPulse=-10,lastTestGuard=-10;
        void ReadInput(float song){
            bool down=false,guard=false;
#if UNITY_EDITOR
            down=Keyboard.current?.spaceKey.isPressed==true||Mouse.current?.leftButton.isPressed==true;
            guard=Keyboard.current?.leftShiftKey.isPressed==true||Mouse.current?.rightButton.isPressed==true;
#endif
            if(down&&!pressed&&Time.unscaledTime-lastInput>.28f){lastInput=Time.unscaledTime;Submit(false);float accuracy=Mathf.Clamp01(1-MusicReactiveSystem.Sample(song).beatDistance/.2f);pulseAt=song;pulseColor=RootKeys.Slots[Mathf.Clamp(session.Slot,0,9)].color;pulseOrigin=Camera.main?Camera.main.transform.position:Vector3.zero;pulseOrigin.y=.07f;Message(accuracy>.7f?"IN RESONANCE": "PULSE",pulseColor);}
            if(!inputRig)inputRig=FindFirstObjectByType<TrackedRig>();if(inputRig&&inputRig.Hands)guard|=inputRig.Hands.Left.closed||inputRig.Hands.Right.closed;
            if(guard&&Time.unscaledTime-lastGuard>.2f){lastGuard=Time.unscaledTime;Submit(true);}pressed=down;
        }
        void Submit(bool guard,bool beam=false,Vector3 origin=default,Vector3 direction=default,int hand=0){if(session.Slot<0)return;var a=new ConcertAction{epoch=session.InputEpoch,run=run,slot=session.Slot,time=now,sequence=++actionSequence,guard=guard,beam=beam,origin=origin,direction=direction,hand=hand};if(Authority)Apply(net?net.ClientNo:-1,a);else if(net&&net.IsReady)net.Rpc("ll.concert.input",new[]{JsonUtility.ToJson(a)},host);}
        void Apply(int sender,ConcertAction a){
            if(a==null||!active||a.run!=run||a.slot<0||a.slot>=10||Mathf.Abs(a.time-now)>.75f||!float.IsFinite(a.time))return;
            if(net&&sender>=0&&sender!=net.ClientNo&&net.GetClientVariable("ll.concert",sender)!="10")return;
            int owner=ParticipantInputSequence.OwnerOf(session.Participants,a.slot);
            if(sender<0){if(a.slot!=session.Slot)return;owner=sender;}
            if(a.beam&&!PalmBeamTiming.Allowed(songNow))return;
            if(a.beam&&!ManualResonanceBeams.ValidRay(a.origin,a.direction,Encounter.heads[a.slot]))return;
            string epoch=net&&sender!=net.ClientNo?net.GetClientVariable(NetworkSessionController.InputEpochKey,sender):session.InputEpoch;
            if(!inputSequences.TryAccept(run,a.run,a.slot,sender,owner,a.epoch,epoch,a.sequence))return;
            if(a.guard)Encounter.guards[a.slot]=now+.38f;else if(a.beam)Encounter.Beam(a.slot,now,Mathf.Clamp01(1-MusicReactiveSystem.Current.beatDistance/.2f),a.origin,a.direction,a.hand);else Encounter.Pulse(a.slot,now,Mathf.Clamp01(1-MusicReactiveSystem.Current.beatDistance/.2f));
        }
        void Rpc(int sender,string method,string[] args){if(args==null||args.Length!=1||args[0].Length>20000)return;try{
            if(method=="ll.concert.input"&&Authority)Apply(sender,JsonUtility.FromJson<ConcertAction>(args[0]));
            else if(method=="ll.concert.state"&&sender==host&&!Authority){var s=JsonUtility.FromJson<ConcertState>(args[0]);if(s!=null&&float.IsFinite(s.dragonHitAt)&&ManualResonanceBeams.Finite(s.dragonHitPoint)&&s.dragonHitSerial>=0&&s.run==run&&s.sequence>lastSnapshot&&Counterattack38.Valid(s.counters)&&s.orcs?.Length==12&&s.dragonShots?.Length==DragonFireballScore.Count&&Array.TrueForAll(s.dragonShots,x=>x!=null&&x.slot>=-1&&x.slot<10&&x.blockerSlot>=-1&&x.blockerSlot<10&&float.IsFinite(x.impactTime)&&ManualResonanceBeams.Finite(x.impactPoint)&&ManualResonanceBeams.Finite(x.impactNormal)&&ManualResonanceBeams.Finite(x.origin)&&ManualResonanceBeams.Finite(x.target))&&Mathf.Abs(s.time-now)<1){lastSnapshot=s.sequence;Encounter.Accept(s);}}
            else if(method=="ll.concert.contact"&&sender==host&&!Authority){var c=JsonUtility.FromJson<ConcertContact>(args[0]);if(c!=null&&c.run==run&&c.sequence>lastContactSequence&&Mathf.Abs(c.time-now)<.75f){lastContactSequence=c.sequence;ContactFeedback(c.slot,c.blocked);}}
        }catch(ArgumentException){}}
        void Message(string message,Color color){feedback.text=message;feedback.color=color;feedbackUntil=Time.unscaledTime+1.1f;}
        void Impact(int slot,bool blocked){
            if(net&&net.IsReady&&Authority)net.Rpc("ll.concert.contact",new[]{JsonUtility.ToJson(new ConcertContact{run=run,slot=slot,sequence=++contactSequence,time=now,blocked=blocked})});
            ContactFeedback(slot,blocked);
        }
        void ContactFeedback(int slot,bool blocked){if(slot!=session.Slot)return;
            if(blocked&&Camera.main){
                var camera=Camera.main.transform;Vector3 centre=camera.position+camera.forward*.75f-camera.up*.35f;
                if(inputRig&&inputRig.Hands){var left=inputRig.Hands.Left;var right=inputRig.Hands.Right;
                    if(left.tracked&&left.closed)centre=left.position+left.direction*.10f;else if(right.tracked&&right.closed)centre=right.position+right.direction*.10f;}
                guardContact.Trigger(centre,camera.forward,now);
            }
            Message(blocked?"HARMONIC GUARD":"CLOSE YOUR HAND TO GUARD",blocked?Color.cyan:new Color(1,.45f,.15f));}
        void DrawFeedback(float t,bool showing){
            if(guardContact)guardContact.Render(now,showing&&Application.isFocused);
            float age=t-pulseAt;bool visible=showing&&age>=0&&age<1;pulseRing.enabled=visible;
            if(visible){float r=.15f+age*9;for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;pulseRing.SetPosition(i,pulseOrigin+new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r));}pulseRing.startColor=pulseRing.endColor=pulseColor*(1-age)*2;}
            if(inputRig&&inputRig.Hands&&PalmBeamTiming.Teaching(t)){var l=inputRig.Hands.Left;var r=inputRig.Hands.Right;if(t>=21.35f&&t<23&&(l.tracked&&l.open||r.tracked&&r.open))learnedGestures|=1;if(t>=23&&t<25&&(l.tracked&&l.closed||r.tracked&&r.closed))learnedGestures|=2;if(t>=25&&FistShield36.Join(false,l,r))learnedGestures|=4;}
            bool teach=showing&&PalmBeamTiming.Teaching(t)&&learnedGestures!=7;demonstration.Render(t,teach?BattleScore.W(21,21.4f,26.5f,27,t):0);bool finale=showing&&t>=111&&t<114;bool rescue=showing&&now>=75&&now<81;bool unite=showing&&now>=92&&now<98;
            bool incoming=false;foreach(var shot in Encounter.State.dragonShots)if(shot.armed&&!shot.resolved&&shot.slot==session.Slot)incoming=true;
            bool message=showing&&!teach&&Time.unscaledTime<feedbackUntil;
            feedback.gameObject.SetActive(showing&&(teach||finale||message||rescue||unite||incoming));
            if(!feedback.gameObject.activeSelf)return;
            if(!message){
                feedback.color=new Color(.82f,.92f,1,1);
                if(finale)feedback.text=Encounter.State.supportMask!=0?"TOGETHER, YOU FREED THE REALM":"THE DISCORD IS BROKEN";
                else if(incoming)feedback.text="FIREBALL INCOMING\nCLOSE YOUR HAND TO GUARD";
                else if(rescue)feedback.text="LEND JERU YOUR LIGHT\nPALMS TOWARD HIS SHIELD";
                else if(unite)feedback.text="ONE FREQUENCY\nPALMS TOWARD THE DRAGON’S HEART";
                else if(teach)feedback.text="OPEN PALMS  ·  BEAM\nFISTS  ·  SHIELDS\nFISTS TOGETHER  ·  LARGE SHIELD";
            }
            float fade=message?Mathf.Clamp01((feedbackUntil-Time.unscaledTime)/.22f):teach?Mathf.Min(Mathf.Clamp01((t-21)/.35f),Mathf.Clamp01((27-t)/.45f)):rescue||unite||incoming?1:Mathf.Clamp01((t-111)/.5f);
            var color=feedback.color;color.a=fade;feedback.color=color;
            cueCard.Progress(rescue?Encounter.State.rescueCharge:unite?Encounter.State.finaleCharge:0);
            cueCard.Appearance(incoming?new Color(1,.4f,.12f):new Color(.62f,.72f,.82f),fade,rescue||unite);
            if(Camera.main){var camera=Camera.main.transform;Vector3 forward=Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;
                if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
                feedback.transform.position=camera.position+forward*2.2f+Vector3.down*.9f;feedback.transform.rotation=Quaternion.LookRotation(forward);
            }
        }
        void OnDestroy(){if(net)net.OnRPCReceived.RemoveListener(Rpc);if(Encounter!=null)Encounter.Impact-=Impact;if(nav.valid)nav.Remove();if(ringMaterial){if(Application.isPlaying)Destroy(ringMaterial);else DestroyImmediate(ringMaterial);}}
    }
}
