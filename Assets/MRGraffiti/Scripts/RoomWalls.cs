using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.XR.PXR;
using Unity.XR.OpenXR.Features.PICOSupport;
namespace MRGraffiti {
 public sealed class RoomWalls:MonoBehaviour {
  public readonly List<PaintWall> walls=new();public Shader paintShader;public Transform trackingSpace;
  public string Status {get;private set;}="Looking for saved walls";public bool Mapping=>manualStep>=0;
  public bool Busy {get;private set;}public int ManualStep=>manualStep;
  readonly CancellationTokenSource cancellation=new();bool dead,provider;int manualStep=-1;Vector3 first,second;float locateAt;
  public async void Query(bool capture){
   if(Busy)return;Busy=true;
   try{
#if UNITY_ANDROID && !UNITY_EDITOR
    if(!PICOSceneCapture.isEnable){Status="No room data: use MAP WALL";return;}
    bool answered=false,allowed=false;PXR_PermissionRequest.RequestUserPermissionMR(_=>answered=true,_=>{allowed=true;answered=true;},_=>answered=true);
    while(!answered&&!dead)await Task.Delay(100,cancellation.Token);if(dead||!allowed){Status="Spatial permission unavailable: use MAP WALL";return;}
    var result=await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SceneCapture,cancellation.Token);if(dead)return;
    if(result!=PxrResult.SUCCESS){Status="Use MAP WALL to align a canvas";return;}provider=true;
    if(capture){Status="Complete PICO room capture";await PXR_MixedReality.StartSceneCaptureAsync(cancellation.Token);if(dead)return;}
    var query=await PXR_MixedReality.QuerySceneAnchorAsync(new[]{PxrSemanticLabel.Wall},cancellation.Token);if(dead)return;
    if(query.result==PxrResult.SUCCESS)foreach(var handle in query.anchorHandleList){
     if(walls.Count>=4)break;if(walls.Exists(w=>w.anchor==handle))continue;
     if(PXR_MixedReality.LocateAnchor(handle,out var p,out var r)!=PxrResult.SUCCESS)continue;
     if(PXR_MixedReality.GetSceneBox2DData(handle,out var offset,out var extent)!=PxrResult.SUCCESS)continue;
     if(Mathf.Abs(Vector3.Dot(r*Vector3.forward,Vector3.up))>.4f||extent.x<.3f||extent.y<.3f)continue;
     Vector2 size=new Vector2(Mathf.Min(6,extent.x),Mathf.Min(4,extent.y));
     var wall=Add(p+r*new Vector3(offset.x,offset.y,0),r,size);wall.anchor=handle;
    }
#endif
    Status=walls.Count>0?"Wall ready — pull trigger to spray":"No saved walls — choose MAP WALL";
   }catch(OperationCanceledException){}catch(Exception e){Status="Room query unavailable — use MAP WALL";Debug.LogWarning("GRAFFITI_ROOM "+e.Message);}finally{Busy=false;}
  }
  public PaintWall Add(Vector3 p,Quaternion r,Vector2 size){var wall=new PaintWall(trackingSpace,p,r,size,paintShader);walls.Add(wall);return wall;}
  public void BeginManual(){if(walls.Count>=4){Status="Four canvases already active";return;}manualStep=0;Status="MAP WALL: place nozzle at lower LEFT; squeeze grip";}
  public bool Mark(Vector3 world){
   if(manualStep==0){first=world;manualStep=1;Status="Place nozzle at lower RIGHT; squeeze grip";return false;}
   if(manualStep==1){if(Vector3.Distance(first,world)<.3f){Status="Choose points at least 30 cm apart";return false;}second=world;manualStep=2;Status="Place nozzle above LEFT point; squeeze grip";return false;}
   if(manualStep!=2)return false;
   Vector3 x=(second-first).normalized,y=Vector3.ProjectOnPlane(world-first,x);float width=Vector3.Distance(first,second),height=y.magnitude;
   if(height<.3f||width>6||height>4){Status="Canvas must be 0.3–6 m wide and 0.3–4 m tall";return false;}y.Normalize();Vector3 n=Vector3.Cross(x,y);
   if(Mathf.Abs(Vector3.Dot(n,Vector3.up))>.4f){Status="Choose a vertical wall; try the upper point again";return false;}
   var centre=first+x*width*.5f+y*height*.5f;var rotation=Quaternion.LookRotation(n,y);
   Add(trackingSpace.InverseTransformPoint(centre),Quaternion.Inverse(trackingSpace.rotation)*rotation,new Vector2(width,height));manualStep=-1;Status="Canvas aligned — paint stays on this wall";return true;
  }
  public bool Hit(Ray ray,float range,out PaintWall wall,out Vector2 uv,out Vector3 point,out float distance,out float incidence){wall=null;uv=default;point=default;distance=range;incidence=0;foreach(var w in walls)if(w.Hit(ray,distance,out var u,out var p,out float d,out float a)){wall=w;uv=u;point=p;distance=d;incidence=a;}return wall!=null;}
  void Update(){
#if UNITY_ANDROID && !UNITY_EDITOR
   if(Time.unscaledTime<locateAt||!provider)return;locateAt=Time.unscaledTime+1;
   foreach(var w in walls)if(w.anchor!=0&&PXR_MixedReality.LocateAnchor(w.anchor,out var p,out var r)==PxrResult.SUCCESS){
    if(PXR_MixedReality.GetSceneBox2DData(w.anchor,out var offset,out var extent)==PxrResult.SUCCESS){w.root.localPosition=p+r*new Vector3(offset.x,offset.y,0);w.root.localRotation=r;}
   }
#endif
  }
  void OnDestroy(){dead=true;cancellation.Cancel();cancellation.Dispose();if(provider)PXR_MixedReality.StopSenseDataProvider(PxrSenseDataProviderType.SceneCapture);foreach(var w in walls)w.Dispose();walls.Clear();}
 }
}
