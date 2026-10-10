using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
namespace LordLyrical
{
    // Tracking only: show choreography never moves the physical XR origin.
    [DefaultExecutionOrder(-200)]
    public class TrackedRig:MonoBehaviour
    {
        public Transform head,left,right;
        public HandPoseInput Hands {get;private set;}
        public TrackedHandMesh LeftVisual {get;private set;}
        public TrackedHandMesh RightVisual {get;private set;}
        void EnsureHandVisuals(){if(!Application.isPlaying)return;if(!LeftVisual)LeftVisual=CreateHandVisual(true);if(!RightVisual)RightVisual=CreateHandVisual(false);}
        TrackedHandMesh CreateHandVisual(bool left){var prefab=Resources.Load<GameObject>("LordLyrical/Tracked"+(left?"Left":"Right")+"Hand");return prefab?Instantiate(prefab,transform,false).GetComponent<TrackedHandMesh>():null;}
        public bool HeadTracked=>Tracked(XRNode.Head);
        public bool LeftTracked=>Hands&&Hands.Left.tracked;
        public bool RightTracked=>Hands&&Hands.Right.tracked;
        static bool Tracked(XRNode node){var d=InputDevices.GetDeviceAtXRNode(node);return d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool value)&&value;}
        void OnEnable(){Hands=GetComponent<HandPoseInput>();if(!Hands)Hands=gameObject.AddComponent<HandPoseInput>();EnsureHandVisuals();if(!GetComponent<HandPresence39>())gameObject.AddComponent<HandPresence39>();Application.onBeforeRender+=Track;}
        void OnDisable(){Application.onBeforeRender-=Track;if(LeftVisual)LeftVisual.Hide();if(RightVisual)RightVisual.Hide();}
        void Update()
        {
            Track();
#if UNITY_EDITOR || UNITY_STANDALONE
            if(!InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid&&Keyboard.current!=null)
            {
                Vector2 move=Vector2.zero;var k=Keyboard.current;
                if(k.wKey.isPressed)move.y++;if(k.sKey.isPressed)move.y--;if(k.aKey.isPressed)move.x--;if(k.dKey.isPressed)move.x++;
                transform.position+=transform.TransformDirection(new Vector3(move.x,0,move.y))*Time.deltaTime*2;
                if(k.qKey.isPressed)transform.Rotate(Vector3.up,-45*Time.deltaTime);if(k.eKey.isPressed)transform.Rotate(Vector3.up,45*Time.deltaTime);
            }
#endif
        }
        [BeforeRenderOrder(100)]
        void Track(){Apply(XRNode.Head,head);if(Hands)Hands.Refresh(this);if(LeftVisual)LeftVisual.UpdatePose(Hands);if(RightVisual)RightVisual.UpdatePose(Hands);}
        static void Apply(XRNode node,Transform target)
        {if(target==null)return;var device=InputDevices.GetDeviceAtXRNode(node);if(device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,out var p))target.localPosition=p;if(device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation,out var r))target.localRotation=r;}
    }
}

