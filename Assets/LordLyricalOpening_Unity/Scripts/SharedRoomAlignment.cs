using UnityEngine;
using UnityEngine.XR;
namespace LordLyrical
{
    [DefaultExecutionOrder(-100)]
    public sealed class SharedRoomAlignment : MonoBehaviour
    {
        public ExperienceDirector director;
        public TrackedRig rig;
        public bool Aligned {get;private set;}
        public float HoldProgress {get;private set;}
        TextMesh instruction;SpatialInstructionCard card;
        bool latched,wasReplay;
        public static Vector3 VenueMark=>new Vector3(0,0,2);
        void Start()
        {
            if(!rig)rig=GetComponent<TrackedRig>();
            var label=new GameObject("Shared room alignment");
            label.transform.SetParent(rig.head,false);label.transform.localPosition=new Vector3(0,-.32f,1.35f);
            instruction=label.AddComponent<TextMesh>();instruction.anchor=TextAnchor.MiddleCenter;instruction.alignment=TextAlignment.Center;
            instruction.characterSize=.012f;instruction.fontSize=64;instruction.color=new Color(.8f,.94f,1);instruction.lineSpacing=1.15f;
            card=label.AddComponent<SpatialInstructionCard>();card.Initialize(instruction);
        }
        public static void AlignOrigin(Transform origin,Transform head)
        {
            Vector3 forward=Vector3.ProjectOnPlane(head.forward,Vector3.up);
            if(forward.sqrMagnitude<.1f)return;
            float yaw=Vector3.SignedAngle(forward.normalized,Vector3.forward,Vector3.up);
            origin.rotation=Quaternion.AngleAxis(yaw,Vector3.up)*origin.rotation;
            Vector3 offset=VenueMark-head.position;offset.y=0;
            origin.position+=offset;
        }
        void LateUpdate()
        {
            if(!rig||!rig.head||!director)return;
            bool replay=director.session&&director.session.CanReplayOffline;
            bool canAlign=director.Phase==SessionPhase.Ready||director.Phase==SessionPhase.WaitingForAssignment||replay;
            bool handAccess=rig.Hands&&rig.Hands.PermissionGranted;
            if(!handAccess)Aligned=false;
            bool handsVisible=handAccess&&rig.Hands.Left.tracked&&rig.Hands.Right.tracked;
            bool held=handsVisible&&rig.Hands.Left.closed&&rig.Hands.Right.closed;
            if(replay&&!wasReplay){latched=true;HoldProgress=0;}wasReplay=replay;
            if(!held)latched=false;
            if(canAlign&&held&&!latched){
                HoldProgress+=Time.unscaledDeltaTime/1.2f;
                if(HoldProgress>=1&&Vector3.ProjectOnPlane(rig.head.forward,Vector3.up).sqrMagnitude>=.1f){
                    AlignOrigin(transform,rig.head);Aligned=true;latched=true;HoldProgress=0;
                    if(director.session)director.session.RoomAligned=true;
                    if(replay)director.session.ReplayOffline();
                }
            }else HoldProgress=0;
            if(director.session)director.session.RoomAligned=Aligned;
            if(!instruction)return;
            instruction.gameObject.SetActive(canAlign);
            card.Progress(Aligned?1:HoldProgress);
            if(replay){card.Progress(HoldProgress);instruction.text="EXPERIENCE COMPLETE\nFace the stage\nHold both fists to replay";return;}
            instruction.text=!handAccess
                ?(rig.Hands&&rig.Hands.PermissionRequestPending?"ALLOW HAND TRACKING\nUse your hands to enter the realm":"ENABLE HAND TRACKING\nLord Lyrical · app permissions")
                :!Aligned
                    ?(director.session&&director.session.offlineDemo?"Stand comfortably\nFace where you want the stage\n":"Stand on the shared floor mark\nFace the stage\n")+(handsVisible?"Hold both fists to align":"Show both hands in front of you")+(HoldProgress>0?"  "+Mathf.CeilToInt(HoldProgress*100)+"%":"")
                    :director.session&&director.session.offlineDemo?"ALIGNED\nStarting your experience":"ALIGNED\nMove to your spot\nThe show will begin together";
        }
        void OnDestroy(){if(instruction)Destroy(instruction.gameObject);}
    }
}
