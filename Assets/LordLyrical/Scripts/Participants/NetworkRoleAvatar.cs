using System.Collections.Generic;
using Styly.NetSync;
using UnityEngine;
using LordLyrical.Characters;
namespace LordLyrical
{
    public class NetworkRoleAvatar:MonoBehaviour
    {
        public GameObject[] participantModels=new GameObject[10];
        static readonly List<NetworkRoleAvatar> Instances=new();
        NetSyncAvatar avatar;ExperienceDirector director;ParticipantAvatarPerformance character;
        Renderer[] modelRenderers;int lastSlot=-1;TrackedRig localRig;int publishedHands=-1,publishedClient=-1,publishedPalms=-1,publishedPalmClient=-1;
        RemotePresence43 presence43;
        MagicalHandPresentation[] auras=new MagicalHandPresentation[2];
        public bool UsesTrackedBody {get;private set;}
        public int AssignedSlot=>lastSlot;
        public static int TrackedCount {get{int count=0;foreach(var item in Instances)if(item&&item.UsesTrackedBody)count++;return count;}}
        public static int TrackedSlotMask {get{int mask=0;foreach(var item in Instances)if(item&&item.UsesTrackedBody&&item.lastSlot>=0)mask|=1<<item.lastSlot;return mask;}}
        public static bool MatchesAssignment(ParticipantRecord p,int client,string device)
            =>p!=null&&p.connected&&p.slot>=0&&p.slot<10&&p.clientNo==client&&!string.IsNullOrEmpty(device)&&p.deviceId==device;
        static bool Valid(Transform target)=>target&&target.gameObject.activeInHierarchy&&float.IsFinite(target.position.x)&&float.IsFinite(target.position.y)&&float.IsFinite(target.position.z);
        static NetworkRoleAvatar Owner(int slot){foreach(var item in Instances)if(item&&item.lastSlot==slot&&item.UsesTrackedBody&&item.avatar&&Valid(item.avatar._head))return item;return null;}
        public static bool TryGetHead(int slot,out Vector3 position){var item=Owner(slot);position=item?item.avatar._head.position:default;return item;}
        public static bool HasTrackedBody(int slot)=>Owner(slot)!=null;
        public static bool TryGetHand(int slot,bool left,out Vector3 position)
        {
            var item=Owner(slot);var hand=item?(left?item.avatar._leftHand:item.avatar._rightHand):null;
            bool tracked=Valid(hand)&&HandPresence39.Tracked(slot,left);position=tracked?hand.position:default;return tracked;
        }
        public static bool TryGetOpenPalm(int slot,bool left,out Vector3 position)
        {
            position=default;var item=Owner(slot);if(!item||!item.director||!item.director.session)return false;
            var manager=item.director.session.Manager;int mask=0;
            if(manager)int.TryParse(manager.GetClientVariable("ll.palms",item.avatar.ClientNo),out mask);
            return HandPresence39.Open(slot,left)&&TryGetHand(slot,left,out position);
        }
        void OnEnable(){if(!Instances.Contains(this))Instances.Add(this);}
        void OnDisable(){Instances.Remove(this);Hide();}
        void Start(){avatar=GetComponent<NetSyncAvatar>();director=FindFirstObjectByType<ExperienceDirector>();}
        void Hide(){presence43?.Clear();foreach(var aura in auras)if(aura)aura.Clear();UsesTrackedBody=false;lastSlot=-1;if(character)character.gameObject.SetActive(false);}
        void LateUpdate()
        {
            if(!avatar||!director||!director.session||!Valid(avatar._head)){Hide();return;}
            int slot=-1;
            // A client variable cannot claim another participant's role.
            foreach(var p in director.session.Participants.participants)
                if(MatchesAssignment(p,avatar.ClientNo,avatar.DeviceId)){slot=p.slot;break;}
            if(slot<0){Hide();return;}
            foreach(var item in Instances)if(item!=this&&item&&item.lastSlot==slot&&item.UsesTrackedBody&&item.avatar&&item.avatar.ClientNo==avatar.ClientNo&&item.GetInstanceID()<GetInstanceID()){Hide();return;}
            if(!character||character.slot!=slot){
                if(character)Destroy(character.gameObject);modelRenderers=null;
                if(participantModels==null||slot>=participantModels.Length||!participantModels[slot]){Hide();return;}
                character=Instantiate(participantModels[slot],transform).GetComponent<ParticipantAvatarPerformance>();
            }
            if(!character){Hide();return;}
            lastSlot=slot;UsesTrackedBody=true;character.gameObject.SetActive(true);
            float time=(float)director.ShowTime;
            character.SetFirstPerson(avatar.IsLocalAvatar);character.SampleTracked(time,ShowFrame.At(time).amplitude);
            var left=Valid(avatar._leftHand)&&HandPresence39.Tracked(slot,true)?avatar._leftHand:null;var right=Valid(avatar._rightHand)&&HandPresence39.Tracked(slot,false)?avatar._rightHand:null;
            if(avatar.IsLocalAvatar){if(!localRig)localRig=FindFirstObjectByType<TrackedRig>();if(localRig){if(!localRig.LeftTracked)left=null;if(!localRig.RightTracked)right=null;}if(localRig&&localRig.Hands){int mask=(localRig.Hands.Left.tracked?1:0)|(localRig.Hands.Right.tracked?2:0);var manager=director.session.Manager;if(manager&&manager.IsReady&&(mask!=publishedHands||publishedClient!=manager.ClientNo)&&manager.SetClientVariable("ll.hands",mask.ToString())){publishedHands=mask;publishedClient=manager.ClientNo;}}}
            else {int mask=0;var manager=director.session.Manager;if(manager)int.TryParse(manager.GetClientVariable("ll.hands",avatar.ClientNo),out mask);mask=(left?1:0)|(right?2:0);character.ApplyTrackedBody(avatar._head,left,right,0,mask);}
            if(avatar.IsLocalAvatar&&localRig&&localRig.Hands){
                int palms=(localRig.Hands.Left.tracked&&localRig.Hands.Left.open?1:0)|(localRig.Hands.Right.tracked&&localRig.Hands.Right.open?2:0);
                var manager=director.session.Manager;if(manager&&manager.IsReady&&(palms!=publishedPalms||publishedPalmClient!=manager.ClientNo)&&manager.SetClientVariable("ll.palms",palms.ToString())){publishedPalms=palms;publishedPalmClient=manager.ClientNo;}
            }
            if(!avatar.IsLocalAvatar)for(int side=0;side<2;side++){
                if(!auras[side]){var go=new GameObject("Remote hand aura");go.transform.SetParent(transform,false);auras[side]=go.AddComponent<MagicalHandPresentation>();auras[side].Initialize(null);}
                int mask=0;var manager=director.session.Manager;if(manager)int.TryParse(manager.GetClientVariable("ll.hands",avatar.ClientNo),out mask);
                var target=side==0?left:right;
                auras[side].Render(Magic33Score.Clock,new HandPoseInput.HandState{tracked=target,position=target?target.position:Vector3.zero,rotation=target?target.rotation:Quaternion.identity,direction=target?target.forward:Vector3.forward},RootKeys.Slots[slot].color,true);
            }
            bool physicalRoom=director.Opening!=null&&director.Opening.RoomOpacity>.01f;
            if(!avatar.IsLocalAvatar){presence43??=new RemotePresence43(transform);presence43.Render(time,physicalRoom?director.Opening.RoomOpacity:0,avatar._head,left,right,RootKeys.Slots[slot].color);}else presence43?.Clear();
            if(modelRenderers==null)modelRenderers=character.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in modelRenderers)if(renderer)renderer.forceRenderingOff=physicalRoom||avatar.IsLocalAvatar;
        }
        void OnDestroy(){presence43?.Dispose();Instances.Remove(this);}
    }
}
