using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LordLyrical.Characters
{
    [DisallowMultipleComponent]
    public sealed class SonicOrcPerformance : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip idle, advance, resonanceStrike, stagger, defeat, retreat;
        public AnimationClip sweep,doubleStrike,thrust,burst,brace;
        public Transform leftWeaponSocket, rightWeaponSocket;
        public bool previewAutomatically;
        public int previewClip;
        public int CurrentClip { get; private set; }
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] players;
        AnimationClip[] clips;
        Renderer[] renderers;Transform[] gripFingers;
        GroundedFeet feet;OrcPhysicalPerformance physical;
        public float MovementSpeed {get;set;}
        sealed class SurfaceStyle
        {
            public Material material;
            public bool resonant;
            public Shader sourceShader,intactShader;
        }
        Material[][] sourceMaterials;
        readonly System.Collections.Generic.List<SurfaceStyle> surfaceStyles=new();
        float lastStrength=float.NaN,lastDissolve=float.NaN;
        public float DissolveAmount=>float.IsNaN(lastDissolve)?0:lastDissolve;

        void EnsureSurfaceStyles()
        {
            if(sourceMaterials!=null)return;
            renderers=GetComponentsInChildren<Renderer>(true);
            sourceMaterials=new Material[renderers.Length][];
            var copies=new System.Collections.Generic.Dictionary<(Material,bool),Material>();
            for(int i=0;i<renderers.Length;i++)
            {
                var renderer=renderers[i];bool resonant=renderer.name.Contains("Resonance");
                var originals=renderer.sharedMaterials;sourceMaterials[i]=originals;
                var assigned=(Material[])originals.Clone();
                for(int j=0;j<originals.Length;j++)
                {
                    var original=originals[j];if(!original)continue;
                    if(!original.HasProperty(Dissolve)&&!original.HasProperty(Emission))continue;
                    var key=(original,resonant);
                    if(!copies.TryGetValue(key,out var copy))
                    {
                        copy=new Material(original){name=original.name+" | Orc instance",hideFlags=HideFlags.HideAndDontSave};
                        copies.Add(key,copy);
                        var intact=original.shader.name=="LordLyrical/Resonant Actor Surface"
                            ?Resources.Load<Shader>("LordLyrical/IntactActorSurface"):null;
                        surfaceStyles.Add(new SurfaceStyle{material=copy,resonant=resonant,sourceShader=original.shader,intactShader=intact});
                    }
                    assigned[j]=copy;
                }
                renderer.sharedMaterials=assigned;
            }
        }
        void ReleaseSurfaceStyles()
        {
            if(sourceMaterials==null)return;
            for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].sharedMaterials=sourceMaterials[i];
            foreach(var style in surfaceStyles)if(style.material)
            {
                if(Application.isPlaying)Destroy(style.material);else DestroyImmediate(style.material);
            }
            surfaceStyles.Clear();sourceMaterials=null;
        }
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
        float[] weights=new float[11],sampleTimes=new float[11];int lastClip=-1;
        Transform[] postBones;Vector3[] basePosition;Quaternion[] baseRotation;
        bool hasBasePose,hasWristPose;Quaternion previousWrist;Transform weaponHand;

        void EnsureGraph()
        {
            if(graph.IsValid()) return;
            if(!animator) animator=GetComponentInChildren<Animator>();
            if(!animator || !idle) return;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion=false;
            // This manual graph owns the pose. A preview controller would overwrite it in Unity's animation update.
            animator.runtimeAnimatorController=null;animator.enabled=true;
            animator.Rebind(); animator.Update(0);
            var bones=new System.Collections.Generic.Dictionary<string,Transform>();foreach(var bone in animator.GetComponentsInChildren<Transform>())bones[bone.name]=bone;
            physical=new OrcPhysicalPerformance(transform,bones,rightWeaponSocket,leftWeaponSocket);
            bones.TryGetValue("hand_r",out weaponHand);
            var post=new System.Collections.Generic.List<Transform>();
            var names=new System.Collections.Generic.HashSet<string>{"pelvis","spine_03","upperarm_r","lowerarm_r","hand_r","upperarm_l","lowerarm_l","hand_l","thigh_l","calf_l","foot_l","thigh_r","calf_r","foot_r"};
            foreach(var pair in bones)if(names.Contains(pair.Key)||(pair.Key.EndsWith("_r")&&(pair.Key.StartsWith("index_")||pair.Key.StartsWith("middle_")||pair.Key.StartsWith("ring_")||pair.Key.StartsWith("pinky_"))))post.Add(pair.Value);
            postBones=post.ToArray();basePosition=new Vector3[postBones.Length];baseRotation=new Quaternion[postBones.Length];hasBasePose=hasWristPose=false;
            if(bones.ContainsKey("foot_l"))feet=new GroundedFeet(transform,bones["thigh_l"],bones["calf_l"],bones["foot_l"],bones["thigh_r"],bones["calf_r"],bones["foot_r"]);
            clips=new[]{idle,advance,resonanceStrike,stagger,defeat,retreat,sweep,doubleStrike,thrust,burst,brace};
            graph=PlayableGraph.Create("Sonic Orc absolute show sampling");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,clips.Length);
            players=new AnimationClipPlayable[clips.Length];
            for(int i=0;i<clips.Length;i++)
            {
                players[i]=AnimationClipPlayable.Create(graph,clips[i]?clips[i]:idle);
                players[i].SetApplyFootIK(false); graph.Connect(players[i],0,mixer,i);
            }
            var output=AnimationPlayableOutput.Create(graph,"Orc skeleton",animator);
            output.SetSourcePlayable(mixer); output.SetWeight(1); graph.Play();
        }

        public void ApplyImpact(ConcertOrc o,float t)=>physical?.Recoil(o,t);
        public void SampleClip(int index,float seconds,float deltaSeconds=-1,bool resetBlend=false)
        {
            EnsureGraph(); if(!graph.IsValid())return;
            index=Mathf.Clamp(index,0,players.Length-1); CurrentClip=index;
            float length=Mathf.Max(.01f,clips[index]?clips[index].length:idle.length);
            float time=index==0||index==1||index==5?Mathf.Repeat(Mathf.Max(0,seconds),length):Mathf.Clamp(seconds,0,length);
            float dt=deltaSeconds>=0?deltaSeconds:Application.isPlaying?Time.deltaTime:0;
            float fade=!resetBlend&&dt>0&&lastClip>=0?1-Mathf.Exp(-dt*22):1;
            for(int i=0;i<players.Length;i++){
                weights[i]=Mathf.Lerp(weights[i],i==index?1:0,fade);
                // End the crossfade exactly: tiny lingering weights otherwise keep old clips evaluating.
                if(i!=index&&weights[i]<.001f)weights[i]=0;
                if(i==index&&weights[i]>.999f)weights[i]=1;
                mixer.SetInputWeight(i,weights[i]);
                float len=Mathf.Max(.01f,clips[i]?clips[i].length:idle.length);
                sampleTimes[i]=i==index?time:sampleTimes[i]+dt;
                if(weights[i]>0)players[i].SetTime(i==0||i==1||i==5?Mathf.Repeat(sampleTimes[i],len):Mathf.Min(sampleTimes[i],len));players[i].SetSpeed(0);
            }lastClip=index;
            // Generic source clips do not key every finger/wrist channel. Restore the
            // unmodified pose before evaluation so grip and IK edits cannot accumulate.
            if(hasBasePose)for(int i=0;i<postBones.Length;i++){postBones[i].localPosition=basePosition[i];postBones[i].localRotation=baseRotation[i];}
            graph.Evaluate(0);
            for(int i=0;i<postBones.Length;i++){basePosition[i]=postBones[i].localPosition;baseRotation[i]=postBones[i].localRotation;}
            hasBasePose=true;
            physical?.Sample(index,time,length);
            if(Application.isPlaying&&(index==0||index==1||index==5))feet?.Evaluate(transform.position.y,MovementSpeed,Time.deltaTime);
            else feet?.Reset();
            if(gripFingers==null){var nodes=GetComponentsInChildren<Transform>(true);var fingers=new System.Collections.Generic.List<Transform>();foreach(var n in nodes)if(n.name.EndsWith("_r")&&(n.name.StartsWith("index_")||n.name.StartsWith("middle_")||n.name.StartsWith("ring_")||n.name.StartsWith("pinky_")))fingers.Add(n);gripFingers=fingers.ToArray();}
            if(weaponHand){
                if(hasWristPose&&!resetBlend&&dt>0)weaponHand.rotation=Quaternion.RotateTowards(previousWrist,weaponHand.rotation,1200*dt);
                previousWrist=weaponHand.rotation;hasWristPose=true;
            }
            foreach(var finger in gripFingers)finger.rotation=Quaternion.AngleAxis(28,transform.right)*finger.rotation;
        }

        float previousEncounter=-1;
        public void SampleEncounter(float time,int index,bool deepSonar,float dissolve)
        {
            bool seek=previousEncounter<0||time<previousEncounter||time-previousEncounter>.5f;
            float dt=seek?0:Mathf.Clamp(time-previousEncounter,0,.05f);int clip;float clock;
            if(index>=9&&time>=45){clip=4;clock=time-45;}
            else if(time>=50){clip=5;clock=time-50+index*.065f;}
            else if(time>=47&&time<49){clip=2;clock=time-47;}
            else if(time>=39&&time<40){clip=3;clock=time-39;}
            else if(time<28){clip=2;clock=Mathf.Max(0,time-26);}
            else {clip=1;clock=time-28+index*.075f;}
            SampleClip(clip,clock,dt,seek);previousEncounter=time;
            SetResonance((deepSonar?3f:1.15f)+.22f*Mathf.Sin(time*3+index),dissolve);
        }

        public void SetResonance(float strength,float dissolve=0)
        {
            // Per-actor materials preserve independent glow/dissolve while retaining
            // URP's SRP Batcher; renderer property blocks disable that path.
            EnsureSurfaceStyles();strength=Mathf.Max(0,strength);dissolve=Mathf.Clamp01(dissolve);
            bool glowChanged=strength!=lastStrength,cutChanged=dissolve!=lastDissolve;
            foreach(var style in surfaceStyles)
            {
                // An intact surface must not compile a discard instruction: it can
                // use normal opaque depth rejection without changing the PBR look.
                if(style.intactShader){
                    var shader=dissolve<=.0001f?style.intactShader:style.sourceShader;
                    if(style.material.shader!=shader)style.material.shader=shader;
                }
                if(cutChanged&&style.material.HasProperty(Dissolve))style.material.SetFloat(Dissolve,dissolve);
                if(glowChanged&&(style.resonant||float.IsNaN(lastStrength))&&style.material.HasProperty(Emission))
                {
                    // Match the previous PropertyBlock.SetColor sRGB-to-linear conversion,
                    // including HDR intensity, without creating a renderer property block.
                    Color emission=style.resonant?new Color(1,.22f,.007f)*strength:Color.black;
                    if(QualitySettings.activeColorSpace==ColorSpace.Linear)emission=emission.linear;
                    style.material.SetVector(Emission,(Vector4)emission);
                }
            }
            lastStrength=strength;lastDissolve=dissolve;
        }
        void Update(){if(previewAutomatically){SampleClip(previewClip,Time.time);SetResonance(1.4f+.4f*Mathf.Sin(Time.time*3));}}
        void OnDisable(){if(graph.IsValid())graph.Destroy();lastClip=-1;previousEncounter=-1;System.Array.Clear(weights,0,weights.Length);}
        void OnDestroy(){if(graph.IsValid())graph.Destroy();ReleaseSurfaceStyles();}
    }
}
