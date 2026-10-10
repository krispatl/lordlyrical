using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LordLyrical.Characters
{
    [DisallowMultipleComponent]
    public sealed class JeruPerformance : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip idle, conduct, raiseTheWord, counterCall, turnToGroup, faceCalibration;
        public bool previewAutomatically;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationLayerMixerPlayable layers;
        HumanoidLocomotion locomotion;
        AvatarMask upperMask;
        GroundedFeet feet;
        JeruGesturePerformance gestures;
        AnimationClipPlayable[] players;
        AnimationClip[] clips;
        SkinnedMeshRenderer[] skins;
        Renderer[] renderers;
        MaterialPropertyBlock block;FacialLodRelay distance;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        void EnsureGraph()
        {
            if(graph.IsValid()) return;
            if(!animator) animator=GetComponentInChildren<Animator>();
            if(!animator || !idle) return;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            // This manual graph owns the pose. A preview controller would overwrite it in Unity's animation update.
            animator.runtimeAnimatorController=null;animator.enabled=true;
            animator.Rebind();
            animator.Update(0);
            clips=new[]{idle,conduct,raiseTheWord,counterCall,turnToGroup,faceCalibration};
            graph=PlayableGraph.Create("Jeru deterministic performance");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,clips.Length);
            players=new AnimationClipPlayable[clips.Length];
            for(int i=0;i<clips.Length;i++)
            {
                players[i]=AnimationClipPlayable.Create(graph,clips[i]?clips[i]:idle);
                players[i].SetApplyFootIK(false);
                graph.Connect(players[i],0,mixer,i);
            }
            var output=AnimationPlayableOutput.Create(graph,"Jeru Animator",animator);
            var library=HumanoidMotionLibrary.Load();
            if(library){
                locomotion=new HumanoidLocomotion(graph,library,animator,transform);
                layers=AnimationLayerMixerPlayable.Create(graph,2);
                graph.Connect(locomotion.Output,0,layers,0);graph.Connect(mixer,0,layers,1);
                layers.SetInputWeight(0,1);layers.SetInputWeight(1,1);
                upperMask=HumanoidLocomotion.UpperBodyMask();layers.SetLayerMaskFromAvatarMask(1,upperMask);
                output.SetSourcePlayable(layers);
            }else output.SetSourcePlayable(mixer);
            output.SetWeight(1);
            animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            graph.Play();
            if(animator.isHuman){feet=new GroundedFeet(animator,transform);gestures=new JeruGesturePerformance(animator,transform);}
            skins=GetComponentsInChildren<SkinnedMeshRenderer>(true);
            renderers=GetComponentsInChildren<Renderer>(true);
            block=new MaterialPropertyBlock();
        }

        float previousShow=-1;int activeClip=-1;float transitionAt;float[] blendStart=new float[6];readonly float[] clipTimes=new float[6];
        // Sampling an absolute time keeps the 121-second show seekable and repeatable.
        public void SampleShow(float time)
        {
            // Neutral breathing supports the reference-led gesture score. The old conducting
            // loop moved shoulders continuously even when a phrase called for stillness.
            int index=0;
            float local=time;
            EnsureGraph();if(!graph.IsValid())return;
            bool seek=previousShow<0||time<previousShow||time-previousShow>.5f;
            if(index!=activeClip){for(int i=0;i<6;i++)blendStart[i]=seek?(i==index?1:0):mixer.GetInputWeight(i);activeClip=index;transitionAt=time;}
            float blend=seek?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((time-transitionAt)/.28f));
            for(int i=0;i<6;i++){
                float len=clips[i]?clips[i].length:idle.length;
                float sample=i==index?local:clipTimes[i]+Mathf.Max(0,time-previousShow);
                clipTimes[i]=sample;
                players[i].SetTime(i<2?Mathf.Repeat(Mathf.Max(0,sample),Mathf.Max(.01f,len)):Mathf.Clamp(sample,0,Mathf.Max(0,len-.001f)));
                players[i].SetSpeed(0);mixer.SetInputWeight(i,Mathf.Lerp(blendStart[i],i==index?1:0,blend));
            }
            if(locomotion!=null){
                Vector3 velocity=ConductorRoute.Velocity(time);
                locomotion.Sample(transform.InverseTransformDirection(velocity),transform.lossyScale.y,ConductorRoute.Distance(time),time,ConductorRoute.TurnRate(time));
            }
            graph.Evaluate(0);
            feet?.Evaluate(transform.position.y,ConductorRoute.Velocity(time).magnitude,Mathf.Clamp(time-previousShow,0,.05f),seek);
            gestures?.Sample(time);
            previousShow=time;
            if(!distance)distance=GetComponent<FacialLodRelay>();if(distance)distance.Synchronize();
        }


        public Vector3 BattlePalmNormal=>gestures!=null?gestures.PalmNormal(1):transform.forward;
        public void ApplyBattle(float t,Vector3 heart,Vector3 shield,float rescue,Vector3 audience){
            gestures?.Battle(t,heart,shield,rescue,audience);if(distance)distance.Synchronize();
        }

        public void SampleArrival(float songTime)
        {
            SampleShow(0);if(!animator||!animator.isHuman)return;
            float impact=OpeningTiming.Smooth(-.91f,-.7f,songTime)*(1-OpeningTiming.Smooth(-.7f,-.1f,songTime));
            float suspended=1-OpeningTiming.Smooth(-1.0f,-.7f,songTime);
            float weight=1-OpeningTiming.Smooth(-.4f,0,songTime);
            var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
            var leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Vector3 lp=leftFoot.position,rp=rightFoot.position;
            hips.position-=transform.up*(.16f*impact*transform.lossyScale.y);
            foreach(bool left in new[]{true,false}){
                var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                var foot=left?leftFoot:rightFoot;Quaternion rotation=foot.rotation;
                AnatomicalIK.Solve(upper,lower,foot,left?lp:rp,AnatomicalIK.PreserveBend(upper,lower,foot,-transform.forward));foot.rotation=rotation;
                var arm=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                var forearm=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                float side=left?1:-1;
                Vector3 target=transform.TransformPoint(new Vector3(side*Mathf.Lerp(.48f,.68f,suspended),Mathf.Lerp(.95f,1.42f,suspended),-.16f-impact*.15f));
                Quaternion handRotation=hand.rotation;
                AnatomicalIK.Solve(arm,forearm,hand,Vector3.Lerp(hand.position,target,weight),-transform.up+transform.right*side*.5f);hand.rotation=handRotation;
            }
            if(!distance)distance=GetComponent<FacialLodRelay>();if(distance)distance.Synchronize();
        }

        public void SampleClip(int index,float seconds)
        {
            EnsureGraph(); if(!graph.IsValid()) return;
            index=Mathf.Clamp(index,0,players.Length-1);
            float length=clips[index]?clips[index].length:idle.length;
            float t=index==0||index==1?Mathf.Repeat(Mathf.Max(0,seconds),Mathf.Max(.01f,length)):Mathf.Clamp(seconds,0,Mathf.Max(0,length-.001f));
            for(int i=0;i<players.Length;i++)
            {
                mixer.SetInputWeight(i,i==index?1:0);
                players[i].SetTime(t);
                players[i].SetSpeed(0);
            }
            if(locomotion!=null)locomotion.Sample(Vector3.zero,transform.lossyScale.y,0,seconds);
            graph.Evaluate(0);if(!distance)distance=GetComponent<FacialLodRelay>();if(distance)distance.Synchronize();
        }

        public void SetExpression(string name,float weight)
        {
            if(skins==null) skins=GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var skin in skins)
            {
                var mesh=skin.sharedMesh;if(!mesh) continue;
                for(int i=0;i<mesh.blendShapeCount;i++)
                {
                    string channel=mesh.GetBlendShapeName(i);
                    if(channel==name || channel.EndsWith("."+name,StringComparison.Ordinal))
                        skin.SetBlendShapeWeight(i,Mathf.Clamp01(weight)*100);
                }
            }
        }

        public void SetSpectrum(Color color,float strength)
        {
            if(renderers==null) renderers=GetComponentsInChildren<Renderer>(true);
            if(block==null) block=new MaterialPropertyBlock();
            foreach(var renderer in renderers)
            {
                if(!renderer.name.Contains("Cymatic")) continue;
                renderer.GetPropertyBlock(block);
                block.SetColor(Emission,color*Mathf.Max(0,strength));
                renderer.SetPropertyBlock(block);
            }
        }

        void Update(){if(previewAutomatically) SampleClip(1,Time.time);}
        void Release(){if(graph.IsValid())graph.Destroy();if(upperMask){if(Application.isPlaying)Destroy(upperMask);else DestroyImmediate(upperMask);}locomotion=null;gestures=null;previousShow=-1;activeClip=-1;}
        void OnDisable(){Release();}
        void OnDestroy(){Release();}
    }
}
