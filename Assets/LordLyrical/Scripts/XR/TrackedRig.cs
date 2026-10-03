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
        void OnEnable(){Application.onBeforeRender+=Track;}
        void OnDisable(){Application.onBeforeRender-=Track;}
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
        void Track(){Apply(XRNode.Head,head);Apply(XRNode.LeftHand,left);Apply(XRNode.RightHand,right);}
        static void Apply(XRNode node,Transform target)
        {if(target==null)return;var device=InputDevices.GetDeviceAtXRNode(node);if(device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,out var p))target.localPosition=p;if(device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation,out var r))target.localRotation=r;}
    }
}

