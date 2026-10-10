using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical {
 // Local cosmetics placed on the validated remote head/hands, never a room mesh.
 public sealed class RemotePresence43:System.IDisposable {
  readonly GameObject root;readonly LineRenderer[] lines=new LineRenderer[4];readonly Material material;readonly Vector3[] points=new Vector3[12];
  public int ActiveLines{get;private set;}
  public RemotePresence43(Transform parent){root=new GameObject("Remote participant | MR colour signature");root.transform.SetParent(parent,false);material=new Material(Shader.Find("LordLyrical/Resonant Filament"));material.SetColor("_Color",Color.white);material.SetFloat("_CoreWhite",.4f);
   for(int i=0;i<4;i++){var g=new GameObject(i<2?"Shoulder signature":"Tracked hand signature");g.transform.SetParent(root.transform,false);var l=lines[i]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=true;l.positionCount=12;l.widthMultiplier=i<2?.006f:.003f;l.enabled=false;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;}
  }
  public void Render(float time,float room,Transform head,Transform left,Transform right,Color colour){
   ActiveLines=0;bool visible=head&&head.gameObject.activeInHierarchy&&ManualResonanceBeams.Finite(head.position)&&room>.002f;
   Vector3 forward=visible?Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized:Vector3.forward;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;Vector3 axis=Vector3.Cross(Vector3.up,forward);
   for(int i=0;i<4;i++){var hand=i==2?left:right;bool on=visible&&(i<2||hand&&hand.gameObject.activeInHierarchy&&ManualResonanceBeams.Finite(hand.position));var l=lines[i];l.enabled=on;if(!on)continue;ActiveLines++;var c=colour;c.a=Mathf.Clamp01(room)*(i<2?.22f:.3f);l.startColor=l.endColor=c;
    Vector3 centre=i<2?head.position-Vector3.up*.34f+axis*(i==0?-.23f:.23f):hand.position;float r=i<2?.11f:.055f;
    for(int j=0;j<12;j++){float a=(j/11f-.5f)*Mathf.PI*1.25f;points[j]=i<2?centre+axis*Mathf.Sin(a)*r+Vector3.up*Mathf.Cos(a)*r:centre+hand.right*Mathf.Cos(a+time*.3f)*r+hand.up*Mathf.Sin(a+time*.3f)*r;}l.SetPositions(points);
   }
  }
  public void Clear(){ActiveLines=0;foreach(var l in lines)if(l)l.enabled=false;}
  public void Dispose(){if(Application.isPlaying){Object.Destroy(root);Object.Destroy(material);}else{Object.DestroyImmediate(root);Object.DestroyImmediate(material);}}
 }
}
