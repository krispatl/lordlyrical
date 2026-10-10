using UnityEngine;
namespace LordLyrical.Characters
{
    public sealed class JeruGesturePerformance
    {
        readonly Animator animator;
        readonly Transform actor,spine,chest,head;
        readonly Transform[] upper=new Transform[2],lower=new Transform[2],hand=new Transform[2],shoulder=new Transform[2];
        readonly Quaternion[] wristCorrection=new Quaternion[2];
        readonly Vector3 forward,right;
        readonly Transform[,] fingers=new Transform[2,15];
        readonly Vector3[,] curlAxis=new Vector3[2,15],spreadAxis=new Vector3[2,15];
        readonly Quaternion[,] fingerRest=new Quaternion[2,15];
        static readonly HumanBodyBones[] leftFingers={HumanBodyBones.LeftThumbProximal,HumanBodyBones.LeftThumbIntermediate,HumanBodyBones.LeftThumbDistal,HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftIndexIntermediate,HumanBodyBones.LeftIndexDistal,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftMiddleIntermediate,HumanBodyBones.LeftMiddleDistal,HumanBodyBones.LeftRingProximal,HumanBodyBones.LeftRingIntermediate,HumanBodyBones.LeftRingDistal,HumanBodyBones.LeftLittleProximal,HumanBodyBones.LeftLittleIntermediate,HumanBodyBones.LeftLittleDistal};
        static readonly HumanBodyBones[] rightFingers={HumanBodyBones.RightThumbProximal,HumanBodyBones.RightThumbIntermediate,HumanBodyBones.RightThumbDistal,HumanBodyBones.RightIndexProximal,HumanBodyBones.RightIndexIntermediate,HumanBodyBones.RightIndexDistal,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightMiddleIntermediate,HumanBodyBones.RightMiddleDistal,HumanBodyBones.RightRingProximal,HumanBodyBones.RightRingIntermediate,HumanBodyBones.RightRingDistal,HumanBodyBones.RightLittleProximal,HumanBodyBones.RightLittleIntermediate,HumanBodyBones.RightLittleDistal};
        public JeruGesturePerformance(Animator animator,Transform actor)
        {
            this.animator=animator;this.actor=actor;
            forward=HumanoidLocomotion.Forward(animator,actor);
            right=Vector3.Cross(Vector3.up,forward).normalized;
            spine=animator.GetBoneTransform(HumanBodyBones.Spine);
            chest=animator.GetBoneTransform(HumanBodyBones.Chest);
            head=animator.GetBoneTransform(HumanBodyBones.Head);
            for(int side=0;side<2;side++){
                bool left=side==0;
                shoulder[side]=animator.GetBoneTransform(left?HumanBodyBones.LeftShoulder:HumanBodyBones.RightShoulder);
                upper[side]=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                lower[side]=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                hand[side]=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                var ids=left?leftFingers:rightFingers;
                for(int i=0;i<15;i++)fingers[side,i]=animator.GetBoneTransform(ids[i]);
                Vector3 direction=(fingers[side,6].position-hand[side].position).normalized;
                Vector3 across=fingers[side,3].position-fingers[side,12].position;
                Vector3 normal=Vector3.Cross(direction,across).normalized;
                if(Vector3.Dot(normal,actor.TransformDirection(forward))<0)normal=-normal;
                wristCorrection[side]=Quaternion.Inverse(Quaternion.LookRotation(direction,normal))*hand[side].rotation;
                for(int i=0;i<15;i++){
                    var f=fingers[side,i];if(!f)continue;
                    Vector3 d=f.childCount>0?f.GetChild(0).position-f.position:direction;
                    fingerRest[side,i]=f.localRotation;
                    curlAxis[side,i]=f.InverseTransformDirection(Vector3.Cross(d.normalized,normal).normalized);
                    spreadAxis[side,i]=f.InverseTransformDirection(normal);
                }
            }
        }
        Vector3 Point(Vector3 p)=>actor.TransformPoint(right*p.x+Vector3.up*p.y+forward*p.z);
        Vector3 Direction(Vector3 p)=>actor.TransformDirection(right*p.x+Vector3.up*p.y+forward*p.z);
        public Vector3 PalmNormal(int side)=>hand[side]?hand[side].rotation*Quaternion.Inverse(wristCorrection[side])*Vector3.up:Vector3.forward;
        public void Battle(float t,Vector3 heart,Vector3 shield,float rescue,Vector3 audience)
        {
            float fight=BattleScore.Duel(t),thanks=BattleScore.W(103,105,110,113,t);
            if(fight<=0&&thanks<=0)return;
            Vector3 worldRight=actor.TransformDirection(right),worldForward=actor.TransformDirection(forward);
            if(fight>0){
                var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
                var lf=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Vector3 lp=lf.position,rp=rf.position;Quaternion lr=lf.rotation,rr=rf.rotation;
                hips.position-=actor.up*((BattleScore.Crouch(t)+.035f*HeroDuelScore.Anticipation(t))*actor.lossyScale.y*fight);
                hips.position+=worldRight*(.035f*(HeroDuelScore.Anticipation(t)-HeroDuelScore.FollowThrough(t))*actor.lossyScale.y*fight);
                foreach(bool left in new[]{true,false}){
                    var u=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                    var l=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                    var f=left?lf:rf;AnatomicalIK.Solve(u,l,f,left?lp:rp,AnatomicalIK.PreserveBend(u,l,f,worldForward));f.rotation=left?lr:rr;
                }
                float prepare=HeroDuelScore.Anticipation(t),strike=BattleScore.Counter(t);
                // Torso leads the hand; the planted lower body continues to own floor contact.
                if(spine)spine.rotation=Quaternion.AngleAxis((-5*prepare+3*HeroDuelScore.FollowThrough(t))*fight,actor.up)*spine.rotation;
                if(chest){
                    chest.rotation=Quaternion.AngleAxis((-13*prepare+9*strike)*fight,actor.up)*chest.rotation;
                    chest.rotation=Quaternion.AngleAxis((8*BattleScore.Shield(t)+5*strike-4*prepare)*(1-rescue*.35f)*fight,worldRight)*chest.rotation;
                }
                float shieldWeight=BattleScore.Shield(t);
                Vector3 guard=Point(new Vector3(-.27f,1.22f,.28f));
                AnatomicalIK.Solve(upper[0],lower[0],hand[0],Vector3.Lerp(hand[0].position,guard,fight*(1-shieldWeight)),-actor.up-worldRight*.5f);
                Aim(0,shield,fight*shieldWeight,.75f);
                float gather=HeroDuelScore.FinalGather(t),release=HeroDuelScore.FinalRelease(t);
                Vector3 gatherPoint=Point(new Vector3(-.20f,1.36f,.38f));
                AnatomicalIK.Solve(upper[0],lower[0],hand[0],Vector3.Lerp(hand[0].position,gatherPoint,gather*fight),-actor.up-worldRight*.55f);
                Aim(0,heart,release*fight,.85f);
                if(shoulder[1])shoulder[1].rotation=Quaternion.AngleAxis((-3*prepare+4*strike)*fight,worldForward)*shoulder[1].rotation;
                if(chest)chest.rotation=Quaternion.AngleAxis((-4*prepare+6*HeroDuelScore.FollowThrough(t))*fight,worldRight)*chest.rotation;
                Aim(1,heart,fight,HeroDuelScore.RightExtension(t));
                // Distinct chambering across five attacks; the firing palm remains exactly aimed.
                float chamber=prepare*(1-strike)*fight;
                Vector3 chamberPoint=Point(new Vector3(.24f+HeroDuelScore.Style(t)*.08f,1.22f+.14f*(1+HeroDuelScore.Style(t)),.17f));
                AnatomicalIK.Solve(upper[1],lower[1],hand[1],Vector3.Lerp(hand[1].position,chamberPoint,chamber),-actor.up+worldRight*.65f);
                // Chambering translates the palm after the arm solve; re-aim the wrist from its final position.
                Vector3 releaseNormal=(heart-hand[1].position).normalized;
                Vector3 releaseFingers=Vector3.ProjectOnPlane(actor.up,releaseNormal).normalized;
                hand[1].rotation=Quaternion.Slerp(hand[1].rotation,Quaternion.LookRotation(releaseFingers,releaseNormal)*wristCorrection[1],fight);
                CombatDigits(0,t,fight,prepare*.35f+gather*.55f);
                CombatDigits(1,t,fight,prepare*.85f+gather*.25f);
            }
            if(thanks>0){
                AnatomicalIK.Solve(upper[0],lower[0],hand[0],Vector3.Lerp(hand[0].position,Point(new Vector3(-.13f,1.40f,.25f)),thanks),-actor.up-worldRight*.4f);
                Aim(1,audience,thanks,.62f);
                if(chest)chest.rotation=Quaternion.AngleAxis(6*thanks,worldRight)*chest.rotation;
            }
            if(head){
                Vector3 target=fight>0?heart:audience;
                var delta=Quaternion.FromToRotation(worldForward,(target-head.position).normalized);
                delta.ToAngleAxis(out float angle,out var axis);if(angle>180)angle-=360;
                head.rotation=Quaternion.AngleAxis(Mathf.Clamp(angle,-32,32)*Mathf.Max(fight,thanks),axis)*head.rotation;
            }
        }
        void CombatDigits(int side,float t,float weight,float tension)
        {
            float follow=HeroDuelScore.FollowThrough(t-.07f);
            // Roll around the palm normal preserves the exact beam aim.
            hand[side].rotation=Quaternion.AngleAxis((side==0?-1:1)*(tension*13-follow*7)*weight,PalmNormal(side))*hand[side].rotation;
            for(int i=0;i<15;i++)if(fingers[side,i]){
                int digit=i/3,joint=i%3;
                float curl=Mathf.Clamp01(tension)*(digit==0?18:joint==1?48:28);
                float spread=(digit-2)*(1-Mathf.Clamp01(tension))*2.1f*(side==0?-1:1);
                var pose=fingerRest[side,i]*Quaternion.AngleAxis(joint==0?spread:0,spreadAxis[side,i])*Quaternion.AngleAxis((digit==0?12:5)+curl+Mathf.Sin(t*2.7f-digit*.55f)*2*(1-tension),curlAxis[side,i]);
                fingers[side,i].localRotation=Quaternion.Slerp(fingers[side,i].localRotation,pose,weight);
            }
        }
        void Aim(int side,Vector3 target,float weight,float extension)
        {
            Vector3 direction=(target-upper[side].position).normalized;
            float reach=Vector3.Distance(upper[side].position,lower[side].position)+Vector3.Distance(lower[side].position,hand[side].position);
            Vector3 desired=upper[side].position+direction*(reach*extension);
            AnatomicalIK.Solve(upper[side],lower[side],hand[side],Vector3.Lerp(hand[side].position,desired,weight),-actor.up+actor.TransformDirection(right)*(side==0?-.4f:.4f));
            Vector3 normal=(target-hand[side].position).normalized;
            Vector3 fingersUp=Vector3.ProjectOnPlane(actor.up,normal).normalized;
            if(fingersUp.sqrMagnitude<.01f)fingersUp=Vector3.ProjectOnPlane(actor.TransformDirection(forward),normal).normalized;
            hand[side].rotation=Quaternion.Slerp(hand[side].rotation,Quaternion.LookRotation(fingersUp,normal)*wristCorrection[side],weight);
            for(int i=0;i<15;i++)if(fingers[side,i])fingers[side,i].localRotation=Quaternion.Slerp(fingers[side,i].localRotation,fingerRest[side,i]*Quaternion.AngleAxis(i<3?12:5,curlAxis[side,i]),weight);
        }
        public void Sample(float t)
        {
            float active=OpeningTiming.Smooth(0,.8f,t)*(1-OpeningTiming.Smooth(119,121,t))*(1-BattleScore.Duel(t));
            bool hasCue=JeruReferenceScore.Active(t,out var cue);
            float gesture=hasCue?JeruReferenceScore.Envelope(cue,t)*cue.power:0;
            float body=hasCue?JeruReferenceScore.Envelope(cue,t+.07f)*cue.power:0;
            Vector3 worldRight=actor.TransformDirection(right);
            // Subtle breath is the only periodic addition. Gesture accents and rests are authored.
            float breath=Mathf.Sin(t*1.37f)*.30f*active;
            float lean=hasCue?(cue.motif==JeruReferenceScore.Motif.Point?4.5f:cue.motif==JeruReferenceScore.Motif.Listen?2.5f:2f)*body:0;
            float yaw=hasCue?cue.gaze*.42f*body:0;
            float preparation=hasCue?BattleScore.W(cue.Start,cue.peak-cue.attack,cue.peak-cue.attack,cue.peak,t)*cue.power:0;
            lean-=preparation*2.2f;yaw-=preparation*(hasCue?cue.gaze*.16f:0);
            if(spine)spine.rotation=Quaternion.AngleAxis((breath+lean*.35f)*active,worldRight)*spine.rotation;
            if(chest){
                chest.rotation=Quaternion.AngleAxis(yaw*active,actor.up)*chest.rotation;
                chest.rotation=Quaternion.AngleAxis(lean*.65f*active,worldRight)*chest.rotation;
            }
            if(head){
                float look=hasCue?cue.gaze*.50f*JeruReferenceScore.Envelope(cue,t+.04f):0;
                float nod=hasCue?(cue.motif==JeruReferenceScore.Motif.Salute?5f:cue.motif==JeruReferenceScore.Motif.Accent?3.5f:1.4f)*gesture:0;
                head.rotation=Quaternion.AngleAxis(look*active,actor.up)*head.rotation;
                head.rotation=Quaternion.AngleAxis(nod*active,worldRight)*head.rotation;
            }
            if(head&&Camera.main){
                float connection=(BattleScore.W(24.8f,25.2f,25.7f,26.2f,t)+BattleScore.W(53,53.4f,54,54.5f,t))*active;
                var aim=Quaternion.FromToRotation(actor.TransformDirection(forward),(Camera.main.transform.position-head.position).normalized);
                aim.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                head.rotation=Quaternion.AngleAxis(Mathf.Clamp(angle,-14,14)*connection,axis)*head.rotation;
                head.rotation=Quaternion.AngleAxis(connection*2.5f,worldRight)*head.rotation;
            }
            for(int side=0;side<2;side++){
                float sign=side==0?-1:1;
                var pose=hasCue?JeruReferenceScore.Hand(cue,side,t):JeruReferenceScore.LivingRest(side,t);
                if(shoulder[side]&&hasCue&&side==cue.side){
                    shoulder[side].rotation=Quaternion.AngleAxis(-sign*gesture*2.2f*active,actor.TransformDirection(forward))*shoulder[side].rotation;
                }
                Vector3 hint=-actor.up+actor.TransformDirection(right)*sign*.45f-actor.TransformDirection(forward)*.06f;
                if(hasCue&&side==cue.side&&cue.motif==JeruReferenceScore.Motif.Listen){
                    var listenHint=-actor.up+actor.TransformDirection(right)*sign*.85f-actor.TransformDirection(forward)*.18f;
                    hint=Vector3.Slerp(hint,listenHint,gesture);
                }
                AnatomicalIK.Solve(upper[side],lower[side],hand[side],Vector3.Lerp(hand[side].position,Point(pose.position),active),hint);
                Vector3 fingerDirection=Direction(pose.fingers).normalized;
                Vector3 palmNormal=Vector3.ProjectOnPlane(Direction(pose.palm),fingerDirection).normalized;
                if(palmNormal.sqrMagnitude<.01f)palmNormal=Vector3.ProjectOnPlane(worldRight,fingerDirection).normalized;
                float flourish=hasCue?Mathf.Sin((t-cue.Start)/(cue.End-cue.Start)*Mathf.PI)*gesture*(cue.motif==JeruReferenceScore.Motif.Accent?18:9)*sign:0;
                var wrist=Quaternion.AngleAxis(flourish,fingerDirection)*Quaternion.LookRotation(fingerDirection,palmNormal)*wristCorrection[side];
                hand[side].rotation=Quaternion.Slerp(hand[side].rotation,wrist,active);
                for(int i=0;i<15;i++){
                    var finger=fingers[side,i];if(!finger)continue;
                    int digit=i/3,joint=i%3;
                    float curl=digit==0?pose.thumb:digit==1?pose.index:pose.curl*(digit==2?.85f:digit==3?1:1.12f);
                    float bend=curl*(digit==0?(joint==0?20:38):(joint==0?46:joint==1?66:38));
                    float spread=joint==0?pose.spread*(digit==0?-14:digit==1?-6:digit==2?-1:digit==3?3:8)*sign:0;
                    var target=fingerRest[side,i]*Quaternion.AngleAxis(spread,spreadAxis[side,i])*Quaternion.AngleAxis(bend+gesture*Mathf.Sin(t*3.1f-digit*.65f)*2.5f,curlAxis[side,i]);
                    finger.localRotation=Quaternion.Slerp(finger.localRotation,target,active);
                }
            }
        }
    }
}
