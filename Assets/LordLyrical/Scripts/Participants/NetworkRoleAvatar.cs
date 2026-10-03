using Styly.NetSync;
using UnityEngine;
namespace LordLyrical
{
    public class NetworkRoleAvatar:MonoBehaviour
    {
        NetSyncAvatar avatar;Material material;int lastSlot=-2;Transform body,head;Mesh bodyMesh,handMesh,haloMesh;
        void Start()
        {
            avatar=GetComponent<NetSyncAvatar>();material=ResonantGeometry.Material("Tracked Root Key",Color.cyan,1);
            bodyMesh=ResonantGeometry.Lathe(new[]{0f,.2f,.8f,1.2f},new[]{.2f,.35f,.27f,.05f});
            handMesh=ResonantGeometry.Lathe(new[]{-.09f,0f,.09f},new[]{.02f,.08f,.04f},8);haloMesh=ResonantGeometry.Ring(.48f,.035f,48);
            body=ResonantGeometry.MeshObject("Physical presence",transform,bodyMesh,material,Vector3.zero,Vector3.one).transform;
            head=ResonantGeometry.MeshObject("Tracked crown",avatar._head,handMesh,material,Vector3.zero,Vector3.one*2).transform;
            ResonantGeometry.MeshObject("Physical floor boundary",body,haloMesh,material,Vector3.up*.02f,Vector3.one);
            ResonantGeometry.MeshObject("Left frequency hand",avatar._leftHand,handMesh,material,Vector3.zero,Vector3.one);
            ResonantGeometry.MeshObject("Right frequency hand",avatar._rightHand,handMesh,material,Vector3.zero,Vector3.one);
        }
        void LateUpdate()
        {
            if(avatar==null||avatar._head==null)return;
            body.gameObject.SetActive(!avatar.IsLocalAvatar);head.gameObject.SetActive(!avatar.IsLocalAvatar);
            body.position=avatar._head.position-Vector3.up*1.4f;body.rotation=Quaternion.Euler(0,avatar._head.eulerAngles.y,0);
            if(NetSyncManager.Instance!=null&&NetSyncManager.Instance.IsReady&&int.TryParse(NetSyncManager.Instance.GetClientVariable("ll.slot",avatar.ClientNo),out var slot)&&slot>=0&&slot<10&&slot!=lastSlot){lastSlot=slot;material.SetColor("_Color",RootKeys.Slots[slot].color);body.localScale=new Vector3(slot<2?1.35f:1,slot<2?.85f:1,1);}
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(bodyMesh!=null)Destroy(bodyMesh);if(handMesh!=null)Destroy(handMesh);if(haloMesh!=null)Destroy(haloMesh);}
    }
}

