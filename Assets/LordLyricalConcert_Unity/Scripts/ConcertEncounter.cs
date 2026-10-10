using System;
using UnityEngine;
using UnityEngine.AI;

namespace LordLyrical
{
    public enum OrcIntent { Watching, Pursuing, Flanking, Telegraph, Strike, Recover, Stagger, Defeated, Retreat, Bracing }
    [Serializable] public sealed class ConcertOrc
    {
        public Vector3 position,velocity,lastHitPoint,lastHitNormal,reactionNormal;public float reactionAt=-100;public int hitSerial,lastHitSlot;public float lastHitTime=-100,defeatedAt=-100;public float yaw,health=100,entered,cooldown,travelled,attackStarted;public int target,attackSerial,contactMask;public OrcIntent intent;public OrcAttack attack;public int cooperativeMask;public float cooperativeAt=-100,cooperativeRewardAt=-100;
    }
    [Serializable] public sealed class CounterCharge38 { public float expires=-100,releaseAt=-100;public int releaseSerial,hand; }
    [Serializable] public sealed class ConcertState
    {
        public int run,sequence;public float time,harmony;public int score,blocks,hits,beamHits,defeated,supportMask,cooperativeBursts;public float rescueCharge,finaleCharge,lastSuccess=-100;public bool rescueAwarded;public Vector3 shieldPoint=new Vector3(0,1.8f,5.7f),dragonHeart=new Vector3(0,8,11);
        public int dragonHitSerial;public float dragonHitAt=-100;public Vector3 dragonHitPoint;
        public CounterCharge38[] counters=Counterattack38.Empty();
        public DragonShot[] dragonShots=DragonFireballScore.Empty();
        public ConcertOrc[] orcs=new ConcertOrc[12];
    }
    public sealed class ConcertEncounter
    {
        public ConcertState State {get;private set;}=new();
        public readonly Vector3[] heads=new Vector3[10];public readonly bool[] occupied=new bool[10];
        public readonly float[] guards=new float[10];
        readonly float[,] contributions=new float[12,10];
        readonly float[] pulses=new float[10],beamTimes=new float[20];readonly int[] reservations=new int[10];
        readonly NavMeshPath[] paths=new NavMeshPath[12];readonly float[] nextPath=new float[12];readonly Vector3[] steer=new Vector3[12];
        public event Action<int,bool> Impact;
        public ConcertEncounter(){Reset(0,0);for(int i=0;i<12;i++)paths[i]=new NavMeshPath();}
        public void Reset(int run,float t){State=new ConcertState{run=run,time=t,harmony=.3f};Array.Clear(shields,0,shields.Length);for(int i=0;i<12;i++){State.orcs[i]=new ConcertOrc{position=new Vector3((i%4-1.5f)*2.8f,0,16+i/4*2.2f),target=i%10,yaw=180,entered=t};nextPath[i]=0;for(int slot=0;slot<10;slot++)contributions[i,slot]=-100;}for(int i=0;i<10;i++){guards[i]=-10;pulses[i]=-10;beamTimes[i*2]=beamTimes[i*2+1]=-100;}}
        public void Accept(ConcertState state){State=state;}
        static void Enter(ConcertOrc o,OrcIntent intent,float t){o.intent=intent;o.entered=t;}
        sealed class ShieldPose {public float until;public Vector3 head;public HandPoseInput.HandState left,right;public bool merged;}
        readonly ShieldPose[] shields=new ShieldPose[10];
        public void SetShield(int slot,float t,Vector3 head,HandPoseInput.HandState left,HandPoseInput.HandState right,bool merged){
            if(slot<0||slot>=10)return;var p=shields[slot]??(shields[slot]=new ShieldPose());bool lost=p.left.tracked&&!left.tracked||p.right.tracked&&!right.tracked;if(lost)Counterattack38.Cancel(State.counters[slot]);p.until=t+.06f;p.head=head;p.left=left;p.right=right;p.merged=merged;if(!left.tracked&&!right.tracked)Counterattack38.Cancel(State.counters[slot]);
        }
        bool ShieldHit(int slot,float t,Vector3 a,Vector3 b,out Vector3 point,out Vector3 normal){
            point=normal=default;var p=shields[slot];if(p==null||p.until<t||!occupied[slot])return false;
            if(p.merged&&p.left.tracked&&p.right.tracked){var fist=(p.left.position+p.right.position)*.5f;normal=ShieldGeometry37.Normal(p.head,fist);return ShieldGeometry37.Intersect(a,b,ShieldGeometry37.Centre(p.head,fist,true),normal,ShieldGeometry37.MergedRadius,out point);}
            for(int side=0;side<2;side++){var h=side==0?p.left:p.right;if(!h.tracked||!h.closed)continue;normal=ShieldGeometry37.Normal(p.head,h.position);if(ShieldGeometry37.Intersect(a,b,ShieldGeometry37.Centre(p.head,h.position,false),normal,ShieldGeometry37.SingleRadius,out point))return true;}
            return false;
        }
        public Vector3 DragonMouth;
        public void SetDuelTargets(Vector3 shield,Vector3 heart){State.shieldPoint=shield;State.dragonHeart=heart;}
        public float RegisterContribution(int orc,int slot,float t)
        {
            if(orc<0||orc>=12||slot<0||slot>=10||!occupied[slot]||!float.IsFinite(t))return 1;
            contributions[orc,slot]=t;int mask=0,count=0;
            for(int i=0;i<10;i++)if(occupied[i]&&t>=contributions[orc,i]&&t-contributions[orc,i]<=.35f){mask|=1<<i;count++;}
            var target=State.orcs[orc];
            if(count<2){target.cooperativeMask=0;return 1;}
            target.cooperativeMask=mask;target.cooperativeAt=t;
            if(t-target.cooperativeRewardAt>=.65f){target.cooperativeRewardAt=t;State.cooperativeBursts++;State.score+=5;}
            return 1+.12f*Mathf.Min(3,count-1);
        }
        public bool Beam(int slot,float t,float accuracy,Vector3 origin,Vector3 direction,int hand=0)
        {
            if(slot<0||slot>=10||hand<0||hand>1||!float.IsFinite(t)||!float.IsFinite(accuracy)||!ManualResonanceBeams.Finite(origin)||!ManualResonanceBeams.Finite(direction)||Mathf.Abs(direction.sqrMagnitude-1)>.1f)return false;
            int index=slot*2+hand;float elapsed=t-beamTimes[index];
            if(elapsed<.075f)return false;
            float dt=elapsed>.3f?.1f:Mathf.Min(elapsed,.12f);beamTimes[index]=t;
            float boost=Counterattack38.Beam(State.counters[slot],t,hand);
            var contact=BeamHitResolver.Trace(State,t,origin,direction);
            if(contact.orc>=0){
                float damage=dt*32*(slot==2?1.25f:slot==0?1.12f:1)*(1+.25f*Mathf.Clamp01(accuracy));
                damage*=boost;damage*=RegisterContribution(contact.orc,slot,t);
                Damage(contact.orc,slot,t,damage,contact.point,contact.normal);
                State.beamHits++;State.harmony=Mathf.Clamp01(State.harmony+dt*.025f);State.score+=2;
                if(slot==3||slot==8)for(int i=0;i<12;i++)if(i!=contact.orc&&State.orcs[i].health>0&&Vector3.Distance(State.orcs[i].position,State.orcs[contact.orc].position)<2.1f)Damage(i,slot,t,damage*.22f,State.orcs[i].position+Vector3.up*1.4f,-direction);
                if(slot==1||slot==7)guards[slot]=Mathf.Max(guards[slot],t+.2f);
                if(slot==4)State.harmony=Mathf.Clamp01(State.harmony+dt*.035f);
                return true;
            }
            bool rescue=t>=75&&t<81,finale=t>=92&&t<98;
            if(rescue||finale){
                Vector3 center=rescue?State.shieldPoint:State.dragonHeart;
                if(ManualResonanceBeams.RaySphere(origin,direction,center,rescue?1.35f:1.7f,out float distance)&&distance<=contact.distance){
                    int people=0;for(int i=0;i<10;i++)if(occupied[i])people++;
                    float gain=dt*.45f*boost/Mathf.Max(1,people);
                    if(rescue)State.rescueCharge=Mathf.Clamp01(State.rescueCharge+gain);else {State.finaleCharge=Mathf.Clamp01(State.finaleCharge+gain);RegisterDragonHit(t,origin+direction*distance);}
                    State.supportMask|=1<<slot;State.lastSuccess=t;State.harmony=Mathf.Clamp01(State.harmony+gain*.12f);State.score+=1;return true;
                }
            }
            return false;
        }
        void RegisterDragonHit(float t,Vector3 point)
        {
            // All participants share one bounded reaction envelope. Sustained beams
            // cannot restart it every frame or scale the animation with player count.
            if(t-State.dragonHitAt<.65f)return;
            State.dragonHitAt=t;State.dragonHitPoint=point;State.dragonHitSerial++;
        }
        void Damage(int index,int slot,float t,float amount,Vector3 point,Vector3 normal)
        {
            var o=State.orcs[index];if(o.health<=0||amount<=0)return;
            o.health=Mathf.Max(0,o.health-amount);o.lastHitTime=t;o.lastHitPoint=point;o.lastHitNormal=normal;o.lastHitSlot=slot;o.hitSerial++;State.lastSuccess=t;
            if(o.health<=0){o.defeatedAt=t;State.defeated++;State.score+=100;Enter(o,OrcIntent.Defeated,t);o.velocity=Vector3.zero;return;}
            // Repeated damage must not restart the reaction animation every tick.
            if(t>=o.cooldown){o.reactionAt=t;o.reactionNormal=normal;Enter(o,OrcIntent.Stagger,t);o.cooldown=t+(slot==5?1.05f:.65f);}
        }
        public bool Pulse(int slot,float t,float accuracy,int aimedTarget=-1)
        {
            if(slot<0||slot>=10||!float.IsFinite(t)||!float.IsFinite(accuracy)||t-pulses[slot]<.28f)return false;pulses[slot]=t;
            accuracy=Mathf.Clamp01(accuracy);State.harmony=Mathf.Clamp01(State.harmony+.018f+accuracy*.025f);State.score+=10+Mathf.RoundToInt(accuracy*40);
            if(t<26||t>=54)return true;
            int best=aimedTarget;float distance=20;
            if(best<0)for(int i=0;i<12;i++){float d=Vector3.Distance(State.orcs[i].position,heads[slot]);if(State.orcs[i].health>0&&d<distance){distance=d;best=i;}}
            if(best<0||best>=12)return true;
            Damage(best,slot,t,Mathf.Lerp(16,36,accuracy),State.orcs[best].position+Vector3.up*1.4f,(heads[slot]-State.orcs[best].position).normalized);return true;
        }
        public void Tick(float t,float dt)
        {
            State.time=t;for(int slot=0;slot<10;slot++)if(!occupied[slot]||shields[slot]!=null&&shields[slot].until<t)Counterattack38.Cancel(State.counters[slot]);TickDragonShots(t);if(t>=81&&!State.rescueAwarded&&State.rescueCharge>=.5f){State.rescueAwarded=true;State.score+=200;}State.harmony=Mathf.Max(.1f,State.harmony-dt*.004f);
            for(int s=0;s<10;s++)reservations[s]=-1;
            for(int i=0;i<12;i++){var o=State.orcs[i];if(o.intent==OrcIntent.Telegraph||o.intent==OrcIntent.Strike)reservations[o.target]=i;}
            for(int i=0;i<12;i++){
                var o=State.orcs[i];o.velocity=Vector3.zero;if(o.health<=0)continue;
                if(t>=50){if(o.intent!=OrcIntent.Retreat)Enter(o,OrcIntent.Retreat,t);o.velocity=Vector3.forward*3.4f;o.position+=o.velocity*dt;o.travelled+=3.4f*dt;o.yaw=Mathf.MoveTowardsAngle(o.yaw,0,dt*220);continue;}
                if(t<26)continue;
                float age=t-o.entered;
                if(o.intent==OrcIntent.Bracing){if(age<.6f)continue;Enter(o,OrcIntent.Watching,t);}
                if(o.intent==OrcIntent.Stagger){if(t<o.cooldown)continue;Enter(o,OrcIntent.Watching,t);}
                if(o.intent==OrcIntent.Telegraph||o.intent==OrcIntent.Strike||o.intent==OrcIntent.Recover){
                    float attackAge=t-o.attackStarted;
                    if(o.intent==OrcIntent.Telegraph&&attackAge>=OrcCombatMotion.Windup(o.attack))Enter(o,OrcIntent.Strike,t);
                    for(int hit=0;hit<OrcCombatMotion.ContactCount(o.attack);hit++){
                        int bit=1<<hit;
                        if((o.contactMask&bit)!=0||attackAge<OrcCombatMotion.ContactTime(o.attack,hit))continue;
                        o.contactMask|=bit;
                        if(!occupied[o.target]||!OrcCombatMotion.Contains(o.attack,o.position,o.yaw,heads[o.target]))continue;
                        bool blocked=guards[o.target]>=t;
                        State.harmony=Mathf.Clamp01(State.harmony+(blocked?.055f:-.09f));
                        if(blocked){State.blocks++;Damage(i,o.target,t,18,o.position+Vector3.up*1.5f,-(heads[o.target]-o.position).normalized);o.cooldown=t+.9f;if(o.health>0)Enter(o,OrcIntent.Stagger,t);}
                        else State.hits++;
                        Impact?.Invoke(o.target,blocked);
                        if(blocked)break;
                    }
                    if(o.intent==OrcIntent.Stagger||o.intent==OrcIntent.Defeated)continue;
                    if(attackAge>=OrcCombatMotion.Duration(o.attack)){o.cooldown=t+1.0f;Enter(o,OrcIntent.Watching,t);}
                    else if(attackAge>=OrcCombatMotion.Windup(o.attack)+OrcCombatMotion.Swing(o.attack)&&o.intent!=OrcIntent.Recover)Enter(o,OrcIntent.Recover,t);
                    continue;
                }
                // This is an active encounter: a turn or an obstacle detour must not
                // erase a connected participant. The navigation path handles the stage.
                int best=-1;float distance=float.PositiveInfinity;
                for(int s=0;s<10;s++)if(occupied[s]&&ManualResonanceBeams.Finite(heads[s])){
                    float d=Flat(heads[s]-o.position).magnitude;
                    float cost=d+(s==o.target?-.6f:0);
                    if(cost<distance){distance=cost;best=s;}
                }
                if(best<0){Enter(o,OrcIntent.Watching,t);continue;}o.target=best;
                Vector3 goal=Flat(heads[best]);float actualDistance=Flat(goal-o.position).magnitude;
                if(actualDistance<2.05f&&reservations[best]<0&&t>=o.cooldown){
                    o.attack=(OrcAttack)((i+o.attackSerial)%5);o.attackSerial++;o.contactMask=0;o.attackStarted=t;
                    var aim=Flat(heads[best]-o.position);o.yaw=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;
                    Enter(o,OrcIntent.Telegraph,t);reservations[best]=i;continue;
                }
                bool flank=reservations[best]>=0||(i%3==2&&actualDistance>4);
                // Distinct waiting positions prevent the whole wave piling into the
                // same corner while another Orc owns the attack window.
                if(flank){float angle=i*2.399963f;goal+=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*2.7f;}
                else goal+=Quaternion.Euler(0,(i%2==0?1:-1)*12,0)*(o.position-goal).normalized*1.7f;
                if(o.intent!=(flank?OrcIntent.Flanking:OrcIntent.Pursuing))Enter(o,flank?OrcIntent.Flanking:OrcIntent.Pursuing,t);
                Vector3 destination=goal;
                if(t>=nextPath[i]){nextPath[i]=t+.4f;
                    if(flank&&NavMesh.SamplePosition(Flat(heads[best]),out var center,1,NavMesh.AllAreas)){
                        // Waiting inside the stage footprint blocks the narrow detour.
                        // Choose a walkable ring point on the participant's side instead.
                        for(int attempt=0;attempt<12;attempt++){
                            float angle=i*2.399963f+attempt*Mathf.PI/6;
                            var candidate=center.position+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*2.7f;
                            if(NavMesh.SamplePosition(candidate,out var ring,.25f,NavMesh.AllAreas)&&!NavMesh.Raycast(center.position,ring.position,out _,NavMesh.AllAreas)){
                                goal=ring.position;break;
                            }
                        }
                    }
                    steer[i]=goal;
                    if(NavMesh.SamplePosition(o.position,out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(goal,out var b,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,paths[i])&&paths[i].status==NavMeshPathStatus.PathComplete){var corners=paths[i].corners;if(corners.Length>1)steer[i]=corners[1];}}
                destination=steer[i];var direction=Flat(destination-o.position);Vector3 step=direction.normalized*Mathf.Min(direction.magnitude,dt*(1.25f+(i%3)*.13f));
                Vector3 separation=Vector3.zero;
                for(int j=0;j<12;j++)if(j!=i&&State.orcs[j].health>0){
                    var d=Flat(o.position-State.orcs[j].position);float range=d.magnitude;
                    if(range<1.4f){
                        if(range<.001f)d=Quaternion.Euler(0,(i+j)*137.5f,0)*Vector3.right*(i<j?1:-1);
                        separation+=d.normalized*Mathf.Clamp01((1.4f-range)/.7f)*2.2f;
                    }
                }
                step=Vector3.ClampMagnitude(step+separation*dt,dt*2.0f);
                // Keep a physical personal-space exclusion around every tracked participant.
                var next=o.position+step;for(int s=0;s<10;s++)if(occupied[s]){var d=Flat(next-heads[s]);if(d.magnitude<1.3f)next=Flat(heads[s])+d.normalized*1.3f;}
                if(NavMesh.SamplePosition(next,out var nav,.55f,NavMesh.AllAreas)){
                    bool clear=true;for(int s=0;s<10;s++)if(occupied[s]&&Flat(nav.position-heads[s]).magnitude<1.3f-.001f){clear=false;break;}
                    if(clear)next=nav.position;
                    else next=o.position;
                }
                var previous=o.position;o.position=new Vector3(Mathf.Clamp(next.x,-10,10),0,Mathf.Clamp(next.z,-6,24));
                o.velocity=dt>.0001f?(o.position-previous)/dt:Vector3.zero;o.travelled+=Vector3.Distance(previous,o.position);
                Vector3 look=o.velocity.sqrMagnitude>.04f?o.velocity:Flat(heads[best]-o.position);if(look.sqrMagnitude>.01f)o.yaw=Mathf.MoveTowardsAngle(o.yaw,Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg,dt*180);
            }
        }
        void TickDragonShots(float t)
        {
            for(int i=0;i<DragonFireballScore.Count;i++){
                var s=State.dragonShots[i];float launch=DragonFireballScore.Launch(i);
                if(!s.armed&&t>=launch-DragonFireballScore.Windup&&t<launch){
                    // Round-robin through connected, tracked slots; never target an absent player.
                    for(int k=0;k<10;k++){int slot=(i+k)%10;if(!occupied[slot]||!ManualResonanceBeams.Finite(heads[slot]))continue;
                        s.slot=slot;s.target=heads[slot]+Vector3.down*.45f;s.origin=DragonMouth;s.armed=true;break;}
                }
                if(!s.armed)continue;
                if(!s.launched&&t>=launch){s.origin=DragonMouth;s.launched=true;}
                if(s.launched&&!s.resolved&&t>=launch){
                    Vector3 a=DragonFireballScore.Position(s,Mathf.Max(0,t-launch-.055f)),b=DragonFireballScore.Position(s,t-launch);
                    int blocker=-1;Vector3 contactPoint=default,contactNormal=default;float nearest=float.PositiveInfinity;
                    for(int slot=0;slot<10;slot++)if(ShieldHit(slot,t,a,b,out var point,out var normal)){float d=(point-a).sqrMagnitude;if(d<nearest){nearest=d;blocker=slot;contactPoint=point;contactNormal=normal;}}
                    if(blocker>=0){Counterattack38.Grant(State.counters[blocker],t);s.resolved=s.blocked=true;s.blockerSlot=blocker;s.impactTime=t;s.impactPoint=contactPoint;s.impactNormal=contactNormal;State.blocks++;State.score+=75;State.harmony=Mathf.Clamp01(State.harmony+.06f);Impact?.Invoke(blocker,true);continue;}
                }
                if(s.launched&&!s.resolved&&t>=launch+DragonFireballScore.Flight){
                    s.resolved=true;
                    bool contact=occupied[s.slot]&&Vector3.Distance(heads[s.slot]+Vector3.down*.45f,s.target)<.75f;
                    s.blocked=contact&&shields[s.slot]==null&&guards[s.slot]>=t;
                    s.impactTime=launch+DragonFireballScore.Flight;s.impactPoint=s.target;s.impactNormal=(s.origin-s.target).normalized;if(s.blocked)s.blockerSlot=s.slot;
                    if(contact){if(s.blocked){State.blocks++;State.score+=75;State.harmony=Mathf.Clamp01(State.harmony+.06f);}else{State.hits++;State.harmony=Mathf.Max(.1f,State.harmony-.06f);}Impact?.Invoke(s.slot,s.blocked);}
                }
            }
        }
        static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);
    }
}
