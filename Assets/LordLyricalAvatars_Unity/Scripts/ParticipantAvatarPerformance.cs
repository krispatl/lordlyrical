using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.XR.Hands;

namespace LordLyrical.Characters
{
    [DisallowMultipleComponent]
    public sealed class ParticipantAvatarPerformance : MonoBehaviour
    {
        [Range(0,9)] public int slot;
        public string roleName, rootKey;
        public Color roleColor=Color.cyan;
        public Animator animator;
        public AnimationClip idle,walk,ability,stagger,rise,hands;
        public LODGroup bodyLod,handLod;
        public SkinnedMeshRenderer[] distanceHands=Array.Empty<SkinnedMeshRenderer>();
        public SkinnedMeshRenderer[] bodyLevels;
        public SkinnedMeshRenderer leftHandMesh,rightHandMesh,energyMesh;
        public bool previewAutomatically;
        public Vector3 leftGripEuler,rightGripEuler;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationMixerPlayable outputMixer;
        HumanoidLocomotion locomotion;
        GroundedFeet feet;
        AnimationClipPlayable[] players;
        AnimationClip[] clips;
        MaterialPropertyBlock block;
        Transform headBone,lh,rh;
        Vector3 leftBindLocal,rightBindLocal;
        Quaternion leftCorrection,rightCorrection,headCorrection;
        bool trackingReady,firstPerson,calibrated,hasPreviousPose;
        int visibilityState=-1;int trackedHandMask;
        float modelEye,bodyYaw,distanceTravelled,previousPoseTime;
        Vector3 previousHead,smoothedVelocity;
        Transform hips;
        readonly Transform[,] trackedFingers=new Transform[2,15];
        readonly Quaternion[,] fingerCorrections=new Quaternion[2,15];
        static readonly XRHandJointID[] fingerJoints={XRHandJointID.ThumbMetacarpal,XRHandJointID.ThumbProximal,XRHandJointID.ThumbDistal,XRHandJointID.IndexProximal,XRHandJointID.IndexIntermediate,XRHandJointID.IndexDistal,XRHandJointID.MiddleProximal,XRHandJointID.MiddleIntermediate,XRHandJointID.MiddleDistal,XRHandJointID.RingProximal,XRHandJointID.RingIntermediate,XRHandJointID.RingDistal,XRHandJointID.LittleProximal,XRHandJointID.LittleIntermediate,XRHandJointID.LittleDistal};

        public float CalibratedScale {get;private set;}=1;
        public float LocomotionSpeed=>smoothedVelocity.magnitude;
        public int CurrentClip {get;private set;}
        public bool IsNarrativelyHidden {get;private set;}
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        static readonly float[][] AbilityCues={new[]{42f},new[]{42f},new[]{39f,81f},new[]{81f},new[]{47f,70f,74f},new[]{45f},new[]{31f,55f,83f},new[]{52f},new[]{50f,90f},new[]{34f}};
        public static int ClipForTime(int role,float t,out float local)
        {
            local=t;
            if(t>=92&&t<103){local=t-92;return 4;}
            if(t>=74&&t<75&&role!=4){local=t-74;return 3;}
            float[] cues=AbilityCues[Mathf.Clamp(role,0,9)];
            foreach(float start in cues)if(t>=start&&t<start+3){local=t-start;return 2;}
            if(t>=30&&t<31){local=t-30;return 3;}
            if(t>=63&&t<64){local=t-63;return 3;}
            if(role<2&&t>=43&&t<46){local=t-43;return 1;}
            return 0;
        }
        void EnsureGraph()
        {
            if(graph.IsValid())return;
            if(!animator)animator=GetComponentInChildren<Animator>();
            if(!animator||!idle)return;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
            // This manual graph owns the pose. A preview controller would overwrite it in Unity's animation update.
            animator.runtimeAnimatorController=null;animator.enabled=true;
            animator.Rebind();animator.Update(0);
            clips=new[]{idle,walk,ability,stagger,rise,hands};
            graph=PlayableGraph.Create("Participant "+rootKey+" deterministic performance");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,clips.Length);players=new AnimationClipPlayable[clips.Length];
            for(int i=0;i<clips.Length;i++){
                players[i]=AnimationClipPlayable.Create(graph,clips[i]?clips[i]:idle);
                players[i].SetApplyFootIK(false);players[i].SetSpeed(0);graph.Connect(players[i],0,mixer,i);
            }
            var output=AnimationPlayableOutput.Create(graph,"Participant Animator",animator);
            var library=HumanoidMotionLibrary.Load();
            if(library){locomotion=new HumanoidLocomotion(graph,library,animator,transform);outputMixer=AnimationMixerPlayable.Create(graph,2);graph.Connect(mixer,0,outputMixer,0);graph.Connect(locomotion.Output,0,outputMixer,1);outputMixer.SetInputWeight(0,1);output.SetSourcePlayable(outputMixer);}
            else output.SetSourcePlayable(mixer);
            output.SetWeight(1);graph.Play();
        }
        public void SampleClip(int index,float seconds)
        {
            EnsureGraph();if(!graph.IsValid())return;
            if(locomotion!=null){outputMixer.SetInputWeight(0,1);outputMixer.SetInputWeight(1,0);}
            index=Mathf.Clamp(index,0,players.Length-1);CurrentClip=index;
            float length=Mathf.Max(.01f,clips[index]?clips[index].length:idle.length);
            float time=index==0||index==1||index==5?Mathf.Repeat(Mathf.Max(0,seconds),length):Mathf.Clamp(seconds,0,length-.0001f);
            for(int i=0;i<players.Length;i++){mixer.SetInputWeight(i,i==index?1:0);players[i].SetTime(time);}
            if(trackingReady){lh.localPosition=leftBindLocal;rh.localPosition=rightBindLocal;}
            graph.Evaluate(0);
        }
        public void SampleShow(float time,float amplitude=1)
        {
            SampleClip(ClipForTime(slot,time,out float local),local);
            IsNarrativelyHidden=slot==5&&time>=26&&time<50;
            ApplyVisibility(IsNarrativelyHidden);
            SetResonance((time<21?.13f:1)*Mathf.Max(.15f,amplitude)*(time>=92&&time<103?2.5f:1));
        }
        public void SetResonance(float strength)
        {
            if(block==null)block=new MaterialPropertyBlock();
            void Glow(Renderer r,float scale){if(!r)return;r.GetPropertyBlock(block);block.SetColor(Emission,roleColor*Mathf.Max(0,strength)*scale);r.SetPropertyBlock(block);}
            Glow(energyMesh,2);Glow(leftHandMesh,.25f);Glow(rightHandMesh,.25f);foreach(var r in distanceHands)Glow(r,.25f);
        }
        public void SetFirstPerson(bool value){firstPerson=value;ApplyVisibility(IsNarrativelyHidden);}
        void ApplyVisibility(bool hidden)
        {
            int state=(firstPerson?1:0)+(hidden?2:0);if(state==visibilityState)return;visibilityState=state;
            if(bodyLod)bodyLod.enabled=!firstPerson&&!hidden;
            if(handLod)handLod.enabled=!firstPerson&&!hidden;
            foreach(var hand in distanceHands)if(hand)hand.enabled=!firstPerson&&!hidden;
            if(bodyLevels!=null)foreach(var body in bodyLevels)if(body)body.enabled=!firstPerson&&!hidden;
            if(energyMesh)energyMesh.enabled=!firstPerson&&!hidden;
            if(leftHandMesh)leftHandMesh.enabled=firstPerson||!hidden;
            if(rightHandMesh)rightHandMesh.enabled=firstPerson||!hidden;
        }
        void EnsureTracking()
        {
            if(trackingReady)return;EnsureGraph();
            if(!animator||!animator.isHuman)return;
            headBone=animator.GetBoneTransform(HumanBodyBones.Head);
            lh=animator.GetBoneTransform(HumanBodyBones.LeftHand);rh=animator.GetBoneTransform(HumanBodyBones.RightHand);
            Quaternion Correction(Transform hand,HumanBodyBones middle,HumanBodyBones index,HumanBodyBones pinky){
                Vector3 f=animator.GetBoneTransform(middle).position-hand.position;
                Vector3 across=animator.GetBoneTransform(index).position-animator.GetBoneTransform(pinky).position;
                Vector3 n=Vector3.Cross(f,across).normalized;
                if(Vector3.Dot(n,transform.forward)<0)n=-n;
                return Quaternion.Inverse(Quaternion.LookRotation(f,n))*hand.rotation;
            }
            leftCorrection=Correction(lh,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftLittleProximal);
            rightCorrection=Correction(rh,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightIndexProximal,HumanBodyBones.RightLittleProximal);
            leftBindLocal=lh.localPosition;rightBindLocal=rh.localPosition;
            Vector3 modelForward=HumanoidLocomotion.Forward(animator,transform);
            headCorrection=Quaternion.Inverse(Quaternion.LookRotation(transform.TransformDirection(modelForward),transform.up))*headBone.rotation;
            modelEye=Mathf.Max(.7f,transform.InverseTransformPoint(headBone.position).y+.07f);
            hips=animator.GetBoneTransform(HumanBodyBones.Hips);
            feet=new GroundedFeet(animator,transform);
            for(int side=0;side<2;side++)for(int i=0;i<15;i++){
                var id=(HumanBodyBones)((int)(side==0?HumanBodyBones.LeftThumbProximal:HumanBodyBones.RightThumbProximal)+i);
                var bone=trackedFingers[side,i]=animator.GetBoneTransform(id);if(!bone)continue;
                var hand=side==0?lh:rh;
                Vector3 direction=bone.childCount>0?bone.GetChild(0).position-bone.position:bone.position-bone.parent.position;
                Vector3 normal=transform.forward;
                fingerCorrections[side,i]=Quaternion.Inverse(Quaternion.LookRotation(direction,normal))*bone.rotation;
            }
            trackingReady=true;
        }
        // Live participants never sample the scripted ability, stagger, vanish or ascent clips.
        public void SampleTracked(float time,float amplitude=1)
        {
            EnsureTracking();if(!trackingReady)return;
            IsNarrativelyHidden=false;ApplyVisibility(false);SetResonance(Mathf.Max(.15f,amplitude));
            if(locomotion==null){SampleClip(0,time);return;}
            outputMixer.SetInputWeight(0,0);outputMixer.SetInputWeight(1,1);
            locomotion.Sample(transform.InverseTransformDirection(smoothedVelocity),transform.lossyScale.y,distanceTravelled,time);
            lh.localPosition=leftBindLocal;rh.localPosition=rightBindLocal;
            graph.Evaluate(0);
        }
        // The XR origin is never moved. These separate hand meshes follow the tracked targets.
        public void ApplyTrackedHands(Transform left,Transform right)
        {
            EnsureTracking();if(!trackingReady)return;
            if(leftHandMesh)leftHandMesh.enabled=left!=null;
            if(rightHandMesh)rightHandMesh.enabled=right!=null;
            if(left){lh.position=left.position;lh.rotation=left.rotation*Quaternion.Euler(leftGripEuler)*leftCorrection;}
            if(leftHandMesh&&left)leftHandMesh.localBounds=new Bounds(leftHandMesh.transform.InverseTransformPoint(left.position),Vector3.one*.7f);
            if(rightHandMesh&&right)rightHandMesh.localBounds=new Bounds(rightHandMesh.transform.InverseTransformPoint(right.position),Vector3.one*.7f);
            if(right){rh.position=right.position;rh.rotation=right.rotation*Quaternion.Euler(rightGripEuler)*rightCorrection;}
        }
        public void ApplyTrackedPalms(HandPoseInput input)
        {
            EnsureTracking();if(!trackingReady||!input)return;
            for(int side=0;side<2;side++){
                bool left=side==0;var state=left?input.Left:input.Right;if(!state.tracked)continue;
                var hand=left?lh:rh;
                if(input.TryJointPose(left,XRHandJointID.Wrist,out var wrist)){
                    hand.position=wrist.position;hand.rotation=wrist.rotation*Quaternion.Euler(0,0,180)*(left?leftCorrection:rightCorrection);
                }
                for(int i=0;i<15;i++)if(trackedFingers[side,i]&&input.TryJointPose(left,fingerJoints[i],out var pose))
                    trackedFingers[side,i].rotation=pose.rotation*Quaternion.Euler(0,0,180)*fingerCorrections[side,i];
            }
        }
        public void ApplyTrackedBody(Transform head,Transform left,Transform right,float floorY,int handTrackingMask=0)
        {
            EnsureTracking();if(!trackingReady||!head)return;trackedHandMask=handTrackingMask;
            if(leftHandMesh)leftHandMesh.enabled=left!=null;if(rightHandMesh)rightHandMesh.enabled=right!=null;
            float now=Time.unscaledTime,dt=Mathf.Clamp(now-previousPoseTime,.001f,.05f);
            Vector3 delta=Vector3.ProjectOnPlane(head.position-previousHead,Vector3.up);
            bool warp=!hasPreviousPose||delta.magnitude>1||now-previousPoseTime>.5f;
            if(!calibrated){
                float eye=head.position.y-floorY;float standing=eye<1.25f?1.7f:Mathf.Clamp(eye,1.35f,2.1f);
                CalibratedScale=Mathf.Clamp(standing/modelEye,.55f,1.65f);calibrated=true;bodyYaw=head.eulerAngles.y;
            }
            if(warp){smoothedVelocity=Vector3.zero;feet.Reset();}
            else{smoothedVelocity=Vector3.Lerp(smoothedVelocity,delta/dt,1-Mathf.Exp(-dt*10));distanceTravelled+=Mathf.Min(delta.magnitude,dt*5);}
            float gaze=head.eulerAngles.y,targetYaw=bodyYaw;
            if(smoothedVelocity.magnitude>.18f)targetYaw=gaze;
            else if(Mathf.Abs(Mathf.DeltaAngle(bodyYaw,gaze))>50)targetYaw=gaze-Mathf.Sign(Mathf.DeltaAngle(bodyYaw,gaze))*25;
            bodyYaw=warp?gaze:Mathf.MoveTowardsAngle(bodyYaw,targetYaw,dt*140);
            transform.SetPositionAndRotation(new Vector3(head.position.x,floorY,head.position.z),Quaternion.Euler(0,bodyYaw-(locomotion!=null?locomotion.ModelYaw:0),0));
            transform.localScale=Vector3.one*CalibratedScale;
            Vector3 leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position,rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            float crouch=Mathf.Clamp(head.position.y-(headBone.position.y+.07f*CalibratedScale),-.85f*CalibratedScale,.12f*CalibratedScale);
            hips.position+=Vector3.up*crouch;
            if(crouch<-.03f){
                AnatomicalIK.Solve(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),animator.GetBoneTransform(HumanBodyBones.LeftFoot),leftFoot,AnatomicalIK.PreserveBend(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),animator.GetBoneTransform(HumanBodyBones.LeftFoot),transform.forward));
                AnatomicalIK.Solve(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg),animator.GetBoneTransform(HumanBodyBones.RightLowerLeg),animator.GetBoneTransform(HumanBodyBones.RightFoot),rightFoot,AnatomicalIK.PreserveBend(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg),animator.GetBoneTransform(HumanBodyBones.RightLowerLeg),animator.GetBoneTransform(HumanBodyBones.RightFoot),transform.forward));
            }
            headBone.rotation=head.rotation*headCorrection;
            SolveArm(true,left);SolveArm(false,right);
            feet.Evaluate(floorY,smoothedVelocity.magnitude,dt,warp);
            previousHead=head.position;previousPoseTime=now;hasPreviousPose=true;
        }
        void SolveArm(bool isLeft,Transform target)
        {
            if(!target)return;
            var upper=animator.GetBoneTransform(isLeft?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            var lower=animator.GetBoneTransform(isLeft?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
            var hand=isLeft?lh:rh;
            Vector3 origin=upper.position,delta=target.position-origin;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,hand.position);
            float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);Vector3 direction=delta.sqrMagnitude<.000001f?transform.forward:delta.normalized;
            Vector3 anatomicalRight=(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position).normalized;
            Vector3 anatomicalForward=Vector3.Cross(anatomicalRight,transform.up).normalized;
            Vector3 bend=Vector3.ProjectOnPlane(-transform.up+anatomicalRight*(isLeft?-.45f:.45f),direction).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(direction,anatomicalForward).normalized;
            float along=(a*a-b*b+d*d)/(2*d);Vector3 elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
            Vector3 end=origin+direction*d;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,end-lower.position)*lower.rotation;
            hand.rotation=target.rotation*(((trackedHandMask&(isLeft?1:2))!=0)?Quaternion.Euler(0,0,180):Quaternion.Euler(isLeft?leftGripEuler:rightGripEuler))*(isLeft?leftCorrection:rightCorrection);
        }
        void Update(){if(previewAutomatically)SampleShow(Time.time,1);}
        void Release(){if(graph.IsValid())graph.Destroy();locomotion=null;trackingReady=false;hasPreviousPose=false;visibilityState=-1;}
        void OnDisable(){Release();}
        void OnDestroy(){Release();}
    }
}
