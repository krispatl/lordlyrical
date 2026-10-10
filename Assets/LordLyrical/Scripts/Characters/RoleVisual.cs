using UnityEngine;
using LordLyrical.Characters;
namespace LordLyrical
{
    public class RoleVisual
    {
        public Transform root,head,leftArm,rightArm;public Material material;readonly int slot;readonly Vector3 home;readonly ParticipantAvatarPerformance authored;
        public bool HasParticipant=>!Application.isPlaying||NetworkRoleAvatar.HasTrackedBody(slot);
        public Vector3 Position=>NetworkRoleAvatar.TryGetHead(slot,out var p)?new Vector3(p.x,0,p.z):root.position;
        public RoleVisual(Transform parent,int index,GameObject prefab=null)
        {
            slot=index;var role=RootKeys.Slots[index];home=role.spawn;
            root=ResonantGeometry.Node(role.archetype,parent,home);
            if(prefab!=null){var instance=Object.Instantiate(prefab,root);authored=instance.GetComponent<ParticipantAvatarPerformance>();if(authored!=null)return;Object.Destroy(instance);}
            material=ResonantGeometry.Material(role.archetype,role.color);
            bool stone=role.faction==Faction.StoneChoir;float h=stone?1.3f:1.75f,w=stone?.48f:.3f;
            var body=ResonantGeometry.Lathe(new[]{0f,.15f,.55f,.9f,1f},new[]{w*.5f,w*.95f,w*.65f,w*1.3f,w*.3f});
            ResonantGeometry.MeshObject("Cymatic vestment",root,body,material,Vector3.up*.25f,new Vector3(1,h,1));
            var crown=ResonantGeometry.Lathe(new[]{0f,.14f,.35f,.47f},new[]{.12f,.22f,.17f,.01f},stone?6:9);
            head=ResonantGeometry.MeshObject("Crown",root,crown,material,new Vector3(0,h+.18f,0),Vector3.one).transform;
            for(int side=-1;side<=1;side+=2)
            {
                var arm=ResonantGeometry.Node(side<0?"Left gesture":"Right gesture",root,new Vector3(side*w*1.2f,h*.85f,0));
                var limb=ResonantGeometry.Lathe(new[]{-.7f,-.5f,-.2f,0f},new[]{.045f,.09f,.12f,.17f},8);
                ResonantGeometry.MeshObject("Frequency arm",arm,limb,material,Vector3.zero,Vector3.one);
                if(side<0)leftArm=arm;else rightArm=arm;
                if(role.faction==Faction.Pitch){var wing=ResonantGeometry.Tube(new[]{new Vector3(side*.2f,1.3f,.1f),new Vector3(side*.75f,1.4f,.3f),new Vector3(side*.5f,.2f,.4f)},new[]{.17f,.16f,.015f},5);ResonantGeometry.MeshObject("Folded wing",root,wing,material,Vector3.zero,Vector3.one);}
                if(role.faction==Faction.Prowl){var ear=ResonantGeometry.Lathe(new[]{0f,.35f},new[]{.1f,0f},3);ResonantGeometry.MeshObject("Feline ear",head,ear,material,new Vector3(side*.13f,.35f,0),Vector3.one);}
                if(role.faction==Faction.Howlers){var horn=ResonantGeometry.Tube(new[]{new Vector3(side*.15f,.2f,0),new Vector3(side*.4f,.6f,0),new Vector3(side*.5f,.75f,.1f)},new[]{.12f,.07f,0f},6);ResonantGeometry.MeshObject("Howler horn",head,horn,material,Vector3.zero,Vector3.one);}
            }
            for(int k=0;k<(stone?3:2);k++)ResonantGeometry.MeshObject("Root halo",root,ResonantGeometry.Ring(.6f+k*.12f,.015f,48),material,new Vector3(0,.015f+k*.012f,0),Vector3.one);
            if(role.faction==Faction.Founders)ResonantGeometry.MeshObject("Founder mantle",root,ResonantGeometry.Ring(.6f,.08f,8),material,Vector3.up*1.6f,Vector3.one).transform.localRotation=Quaternion.Euler(75,0,0);
        }
        public Vector3 Hand(bool left=false)
        {
            if(NetworkRoleAvatar.TryGetHand(slot,left,out var tracked))return tracked;
            if(authored&&authored.animator){var hand=authored.animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);if(hand)return hand.position;}
            return root.position+new Vector3(left?-.4f:.4f,1.4f,.25f);
        }
        public void Render(ShowFrame frame,bool occupied,bool local)
        {
            float t=(float)frame.time;root.gameObject.SetActive(!Application.isPlaying&&!local);
            if(Application.isPlaying){root.position=Position;return;}
            if(authored!=null){root.localPosition=CinematicPosition(slot,t);root.localRotation=Quaternion.Euler(0,-home.x*5,0);if(root.gameObject.activeInHierarchy)authored.SampleShow(t,frame.amplitude*(occupied?1.4f:.75f));return;}
            bool charge=(slot<2&&t>=42&&t<47),cantor=(slot==2||slot==3)&&t>=34&&t<50;
            float dz=charge?Mathf.Sin(Mathf.Clamp01((t-42)/4)*Mathf.PI)*2:cantor?1.2f:0;
            root.localPosition=CinematicPosition(slot,t);
            root.localRotation=Quaternion.Euler(0,-home.x*5,0);
            float gesture=t>=92?Mathf.Clamp01((t-92)/4)*115:charge?75:cantor?45:8+Mathf.Sin(t*.6f+slot)*4;
            leftArm.localRotation=Quaternion.Euler(-gesture,0,-12);rightArm.localRotation=Quaternion.Euler(-gesture,0,12);
            material.SetFloat("_Glow",frame.amplitude*(occupied?1.4f:.75f));material.SetFloat("_Phase",t*.17f+slot);material.SetFloat("_Evolution",0);
            if(slot==5)material.SetFloat("_Glow",t>25&&t<50?.025f:frame.amplitude);
        }
        public static Vector3 CinematicPosition(int slot,float time)
        {
            bool charge=slot<2&&time>=42&&time<47,cantor=(slot==2||slot==3)&&time>=34&&time<50;
            float dz=charge?Mathf.Sin(Mathf.Clamp01((time-42)/4)*Mathf.PI)*2:cantor?1.2f:0;
            return RootKeys.Slots[slot].spawn+new Vector3(0,time<21?Mathf.Sin(time*.6f+slot)*.15f:0,dz);
        }
    }
}
