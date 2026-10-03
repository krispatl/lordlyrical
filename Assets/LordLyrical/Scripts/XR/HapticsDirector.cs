using UnityEngine;
using UnityEngine.XR;
namespace LordLyrical
{
    public class HapticsDirector:MonoBehaviour
    {
        public void Pulse(float strength,float seconds)
        {Send(XRNode.LeftHand,strength,seconds);Send(XRNode.RightHand,strength,seconds);}
        static void Send(XRNode node,float strength,float seconds){var d=InputDevices.GetDeviceAtXRNode(node);if(d.TryGetHapticCapabilities(out var c)&&c.supportsImpulse)d.SendHapticImpulse(0,Mathf.Clamp01(strength),Mathf.Min(seconds,.5f));}
        public void Stop(){InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).StopHaptics();InputDevices.GetDeviceAtXRNode(XRNode.RightHand).StopHaptics();}
    }
}
