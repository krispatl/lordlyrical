using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MRGraffiti;
public static class GraffitiValidation {
 static void Need(bool ok,string message){if(!ok)throw new Exception("GRAFFITI_TEST: "+message);}
 static Texture2D Read(RenderTexture rt){var prior=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();RenderTexture.active=prior;return t;}
 static float Coverage(RenderTexture rt){var t=Read(rt);float sum=0;foreach(var c in t.GetPixels32())sum+=c.a/255f;UnityEngine.Object.DestroyImmediate(t);return sum;}
 public static void Run(GameObject prefab,Shader wallShader,Shader stampShader,Shader solid){
  Directory.CreateDirectory(GraffitiBuilder.Out);bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
  var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  PaintWall wall=null;PaintEngine paint=null;GraffitiPalette palette=null;
  try{
   var root=new GameObject("Test tracking origin");wall=new PaintWall(root.transform,new Vector3(0,1.5f,2),Quaternion.identity,new Vector2(2,2),wallShader);paint=new PaintEngine(stampShader,Shader.Find("MRGraffiti/Wet droplet tips"));
   Need(wall.Hit(new Ray(new Vector3(0,1.5f,1.5f),Vector3.forward),2,out var uv,out var p,out float distance,out float angle)&&Vector2.Distance(uv,Vector2.one*.5f)<.001f,"Surface hit mapping");
   Need(!wall.Hit(new Ray(new Vector3(2,1.5f,1.5f),Vector3.forward),2,out _,out _,out _,out _),"Paint outside wall");Need(!wall.Hit(new Ray(Vector3.zero,Vector3.right),2,out _,out _,out _,out _),"Parallel ray paints");
   var caps=Cap.Defaults;Need(SprayModel.Flow(caps[1],.2f,.3f,1,0)<SprayModel.Flow(caps[1],1,.3f,1,0),"Pressure ignored");Need(SprayModel.Radius(caps[2],.5f)>SprayModel.Radius(caps[0],.5f)*3,"Caps indistinct");Need(SprayModel.Flow(caps[1],1,5,1,0)==0,"Beyond range paints");Need(SprayModel.Flow(caps[1],1,.3f,.25f,0)<SprayModel.Flow(caps[1],1,.3f,1,0),"Angle ignored");
   // The fat footprint must flare more with distance, while the detail caps stay unchanged.
   foreach(float range in new[]{.05f,.3f,.9f}){
    float old=.003f+Mathf.Tan(13*Mathf.Deg2Rad)*range;
    Need(SprayModel.Radius(caps[2],range)>old*2,"Fat cap did not double");
    float normal=.003f+Mathf.Tan(7*Mathf.Deg2Rad)*range;
    Need(Mathf.Abs(SprayModel.Radius(caps[1],range)-normal)<.00001f,"Standard cap changed");
   }
   Need(SprayModel.Radius(caps[2],.9f)/.9f>SprayModel.Radius(caps[2],.1f)/.1f*1.35f,"Fat cap does not flare");
   float[] distances={.08f,.3f,.8f};float previousWidth=0;
   foreach(float range in distances){
    for(int i=0;i<35;i++)paint.Spray(wall,Vector2.one*.5f,Vector3.forward,range,1,1,0,1f/72,caps[2],Color.cyan,0);
    var wide=Read(wall.pigment);int min=1024,max=-1;for(int x=0;x<1024;x++)if(wide.GetPixel(x,512).a>.03f){min=Math.Min(min,x);max=Math.Max(max,x);}
    float width=(max-min+1)/512f;Need(width>previousWidth*1.6f,"Actual GPU cap footprint fails to widen");previousWidth=width;
    File.WriteAllBytes(GraffitiBuilder.Out+"/Fat_"+range.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"m.png",wide.EncodeToPNG());UnityEngine.Object.DestroyImmediate(wide);paint.Clear(new[]{wall});
   }
   Vector2 at=new Vector2(.25f,.7f);for(int i=0;i<20;i++)paint.Spray(wall,at,Vector3.forward,.25f,1,.3f,0,1f/72,caps[1],Color.red,0);
   float weak=Coverage(wall.pigment);Need(weak>0,"GPU deposits no pigment");paint.End();for(int i=0;i<35;i++)paint.Spray(wall,at,Vector3.forward,.25f,1,1,0,1f/72,caps[1],Color.red,1);float strong=Coverage(wall.pigment);Need(strong>weak*1.5f,"No accumulation");
   var tex=Read(wall.pigment);Need(tex.GetPixel(256,717).a>.1f,"Stamp orientation/UV mismatch");File.WriteAllBytes(GraffitiBuilder.Out+"/Paint_Deposition.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
   paint.Undo();Need(Mathf.Abs(Coverage(wall.pigment)-weak)<1,"Undo does not restore pixels");paint.Undo();Need(Coverage(wall.pigment)==0,"First stroke not undone");
   for(int stroke=0;stroke<9;stroke++){paint.Spray(wall,new Vector2(.1f+stroke*.09f,.3f),Vector3.forward,.4f,1,1,.3f,.016f,caps[stroke%3],GraffitiPalette.Colors[stroke],stroke%3);paint.End();}Need(paint.UndoCount==PaintEngine.UndoLimit,"Undo memory unbounded");
   paint.Clear(new[]{wall});
   for(int i=0;i<180;i++)paint.Spray(wall,new Vector2(.5f,.7f),Vector3.forward,.07f,1,1,0,1f/72,caps[2],Color.red,0);
   Need(paint.DripCount>0&&paint.DripCount<=PaintEngine.MaxDrips,"Heavy oversaturation does not form bounded drips");
   Vector2 start=paint.FirstDripUV;paint.End();
   for(int i=0;i<36;i++)paint.Tick(1f/72);
   Vector2 half=paint.FirstDripUV;Need(half.y<start.y-.007f&&Mathf.Abs(half.x-start.x)<.0001f,"Drips do not descend after release");
   paint.Spray(wall,new Vector2(.15f,.2f),Vector3.forward,.2f,1,1,1,1f/72,caps[0],Color.blue,0);paint.End();
   Need(paint.DripCount>0,"New stroke stops existing drips");
   for(int i=0;i<84;i++)paint.Tick(1f/72);
   Need(paint.FirstDripUV.y<half.y-.025f,"Drip growth is invisible");
   var drip=Read(wall.pigment);File.WriteAllBytes(GraffitiBuilder.Out+"/Drip_Deposition.png",drip.EncodeToPNG());UnityEngine.Object.DestroyImmediate(drip);
   paint.Undo();Need(Vector2.Distance(paint.FirstDripUV,half)<.00001f,"Undo does not restore running drip state");
   for(int i=0;i<370;i++)paint.Tick(1f/72);Need(paint.DripCount==0,"Drips never settle");
   paint.Clear(new[]{wall});Need(Coverage(wall.pigment)==0&&paint.UndoCount==0&&paint.DripCount==0,"Clear failed");
   // Review a deliberate series of surface-attached graffiti swashes, with all three finishes.
   for(int row=0;row<3;row++){for(int i=0;i<180;i++){float u=i/179f;var point=new Vector2(.12f+u*.76f,.24f+row*.25f+Mathf.Sin(u*13)*.07f);paint.Spray(wall,point,Vector3.forward,.4f,1,1,.3f,.025f,caps[row],GraffitiPalette.Colors[8+row*2],row);}paint.End();}
   var camera=new GameObject("Graffiti review camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,1.5f,.3f);camera.transform.LookAt(wall.root.position);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.21f,.23f,.25f);camera.nearClipPlane=.01f;camera.cullingMask=1<<28;camera.fieldOfView=70;
   Layer(scene);Render(camera,GraffitiBuilder.Out+"/Wall_Paint_Review.png",1280,960,1);
   wall.root.gameObject.SetActive(false);var can=UnityEngine.Object.Instantiate(prefab);can.transform.position=new Vector3(0,1.5f,1);camera.transform.position=new Vector3(.13f,1.58f,.65f);camera.transform.LookAt(can.transform.position+Vector3.up*.025f);camera.fieldOfView=45;Layer(scene);Render(camera,GraffitiBuilder.Out+"/Can_Review.png",900,1100,1);UnityEngine.Object.DestroyImmediate(can);
   int selected=-1,selections=0;palette=new GraffitiPalette(solid,id=>{selected=id;selections++;});camera.transform.position=new Vector3(0,1.6f,0);camera.transform.rotation=Quaternion.identity;camera.fieldOfView=35;
   root.transform.position=new Vector3(0,1.45f,.6f);palette.Toggle(root.transform,camera);
   Need(palette.Root.parent==root.transform&&GraffitiPalette.Radius*2<.34f,"Palette is not a compact off-hand attachment");
   Vector3 before=palette.Root.position;root.transform.position+=Vector3.right*.07f;palette.Follow(true);Need(Vector3.Distance(palette.Root.position,before+Vector3.right*.07f)<.00001f,"Palette does not follow controller");root.transform.position-=Vector3.right*.07f;palette.Follow(true);
   void Click(Vector2 point){var target=palette.Root.TransformPoint(new Vector3(point.x,point.y,0));Need(palette.Ray(new Ray(camera.transform.position,(target-camera.transform.position).normalized),true),"Palette pointer missed");}
   for(int i=0;i<16;i++){float a=i*22.5f*Mathf.Deg2Rad;Click(new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.125f);Need(selected==i,"Radial colour mapping wrong");}
   Layer(scene);Render(camera,GraffitiBuilder.Out+"/Palette_Review.png",1100,1100,1);
   Click(new Vector2(.034f,-.003f));for(int i=0;i<3;i++){float a=i*120*Mathf.Deg2Rad;Click(new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.125f);Need(selected==20+i,"Cap selection wrong");}
   Render(camera,GraffitiBuilder.Out+"/Caps_Review.png",1100,1100,1);
   Click(new Vector2(-.034f,-.034f));Click(new Vector2(0,.125f));Need(selected==30,"Finish selection wrong");Render(camera,GraffitiBuilder.Out+"/Finish_Review.png",1100,1100,1);
   Click(new Vector2(.034f,-.034f));int priorClicks=selections;Vector2 clearPoint=new Vector2(Mathf.Sin(Mathf.PI/3),Mathf.Cos(Mathf.PI/3))*.125f;Click(clearPoint);Need(selections==priorClicks,"Clear did not require confirmation");Click(clearPoint);Need(selected==41&&selections==priorClicks+1,"Clear confirmation failed");Render(camera,GraffitiBuilder.Out+"/Tools_Review.png",1100,1100,1);
   palette.Follow(false);Need(!palette.Root.gameObject.activeSelf,"Untracked off-hand UI stays visible");palette.Follow(true);Need(palette.Root.gameObject.activeSelf,"Palette does not recover tracking");
   var opposite=new GameObject("Other controller").transform;opposite.position=root.transform.position;palette.Hide();palette.Toggle(opposite,camera);Need(palette.Root.parent==opposite,"Swap hand palette target wrong");
   var pose=new GameObject("Can pose test").AddComponent<GraffitiExperience>();var held=UnityEngine.Object.Instantiate(prefab);held.transform.position=pose.gripOffset;held.transform.rotation=Quaternion.Euler(pose.gripEuler);var jet=held.GetComponentsInChildren<Transform>().First(t=>t.name=="NozzleOrigin");Need(jet.position.y>.07f&&Mathf.Abs(Vector3.Dot(jet.forward,Vector3.up)-.5f)<.001f,"Can nozzle does not point 30 degrees up from controller top");UnityEngine.Object.DestroyImmediate(held);UnityEngine.Object.DestroyImmediate(pose.gameObject);
   foreach(var shader in new[]{wallShader,stampShader,solid,Shader.Find("MRGraffiti/Wet droplet tips")})Need(shader&&shader.isSupported&&!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader error");
   File.WriteAllText(GraffitiBuilder.Out+"/Validation.txt","PASS surface-only ray intersections and bounds; pressure, cap, angle and range model; actual GPU pigment deposition and accumulation; UV orientation; pixel-exact undo; six-snapshot budget; clear; fat cap GPU widening and close taper/distant flare; animated drips after release/new strokes and undo/lifetime; all radial colour/cap choices; clear confirmation; off-hand parenting, motion, tracking loss/recovery and hand swap; upward nozzle/can pose; shader and render reviews.\nNative wall anchoring, controller grip alignment, passthrough and worn-headset performance require physical acceptance.\n");Debug.Log("GRAFFITI_VALIDATION_PASS");
  }finally{palette?.Dispose();paint?.Dispose();wall?.Dispose();EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
 }
 static void Render(Camera camera,string path,int width,int height,int unused){
  var old=RenderTexture.active;var target=camera.targetTexture;var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);Texture2D texture=null;
  try{camera.targetTexture=rt;camera.Render();texture=Read(rt);File.WriteAllBytes(path,texture.EncodeToPNG());}
  finally{camera.targetTexture=target;RenderTexture.active=old;if(texture)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
 static void Layer(Scene scene){foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;}
}
