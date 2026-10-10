using System.Linq;
using UnityEngine;
using LordLyrical.Characters;
namespace LordLyrical
{
    // Samples final local joint rotations in LateUpdate, after Unity's animation phase.
    public sealed class FinalPoseMotion
    {
        sealed class Track {
            public readonly Transform[] bones;
            Quaternion[] previous;
            public Track(Transform root,params string[] names){bones=root.GetComponentsInChildren<Transform>(true).Where(t=>names.Contains(t.name)).ToArray();}
            public float Step(){
                var active=bones.Length>0&&bones[0].gameObject.activeInHierarchy;
                if(previous==null){previous=bones.Select(t=>t.localRotation).ToArray();return 0;}
                float delta=0;
                for(int i=0;i<bones.Length;i++){if(!bones[i])continue;if(active)delta=Mathf.Max(delta,Quaternion.Angle(previous[i],bones[i].localRotation));previous[i]=bones[i].localRotation;}
                return delta;
            }
        }
        readonly ResonantWorld world;
        SonicOrcPerformance[] orcs;
        Track[] orcTracks;
        JeruPerformance jeru;
        Track legs,arms,wings,tail;
        float nextLookup;
        public float OrcWalk,OrcAttack,JeruLeg,JeruArm,DragonWing,DragonTail;
        public int Conflicts;
        public FinalPoseMotion(ResonantWorld world){this.world=world;}
        public void Sample()
        {
            if(!world)return;
            if(Time.unscaledTime>=nextLookup&&(orcs==null||jeru==null||wings==null)){
                nextLookup=Time.unscaledTime+.5f;
                if(orcs==null){orcs=world.GetComponentsInChildren<SonicOrcPerformance>(true);orcTracks=orcs.Select(o=>new Track(o.transform,"calf_l","calf_r","upperarm_r","lowerarm_r")).ToArray();if(orcs.Length==0)orcs=null;}
                if(!jeru){jeru=world.GetComponentInChildren<JeruPerformance>(true);if(jeru){legs=new Track(jeru.transform,jeru.animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).name,jeru.animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).name);arms=new Track(jeru.transform,jeru.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).name,jeru.animator.GetBoneTransform(HumanBodyBones.RightUpperArm).name);}}
                if(wings==null){var dragon=world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Current CyberDragon | chamber performance");if(dragon){wings=new Track(dragon,"wing_upper.L","wing_upper.R","wing_fore.L","wing_fore.R");tail=new Track(dragon,"tail_06","tail_10","neck_03");}}
            }
            Conflicts=0;
            if(orcs!=null)for(int i=0;i<orcs.Length;i++){
                var o=orcs[i];if(!o)continue;float d=orcTracks[i].Step();
                if(!o.gameObject.activeInHierarchy)continue;
                if(o.animator.runtimeAnimatorController)Conflicts++;
                if(o.CurrentClip==1||o.CurrentClip==5)OrcWalk+=d;
                else if(o.CurrentClip==2||o.CurrentClip>=6&&o.CurrentClip<=9)OrcAttack+=d;
            }
            if(jeru){JeruLeg+=legs.Step();JeruArm+=arms.Step();if(jeru.gameObject.activeInHierarchy&&jeru.animator.runtimeAnimatorController)Conflicts++;}
            if(wings!=null){DragonWing+=wings.Step();DragonTail+=tail.Step();}
        }
        public void Clear(){OrcWalk=OrcAttack=JeruLeg=JeruArm=DragonWing=DragonTail=0;}
    }
}
