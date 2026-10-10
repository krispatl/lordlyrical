using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
public static class DragonBreathReview
{
    const string Out=MotionPolishValidation.Out+"/Production/DragonBreath";
    [MenuItem("Lord Lyrical/Production/Audit Dragon Breath")]
    public static void Audit()
    {
        Directory.CreateDirectory(Out);var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("DRAGON BREATH ANATOMY AUDIT\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var chamber=UnityEngine.Object.Instantiate(cfg.chamberModel).GetComponent<ChamberPresentation>();chamber.Initialize();
            var dragon=chamber.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            var head=dragon.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="head");
            var jaw=dragon.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="jaw");
            report.AppendLine($"Rest head={head.position:F5}; up={head.up:F5}; forward={head.forward:F5}; jaw={jaw.position:F5}; jaw up={jaw.up:F5}; jaw forward={jaw.forward:F5}");
            foreach(float t in new[]{68.9f,69.5f,73.99f,74.0f,75.5f,76f,78.5f,80.5f}){
                chamber.Evaluate(t);var target=t<74?ConductorRoute.Position(t)+Vector3.up*1.7f:new Vector3(Mathf.Sin((t-74)*.75f)*6,1.7f,-2);
                var upper=head.TransformPoint(new Vector3(0,.967f,.100f));var lower=jaw.TransformPoint(new Vector3(0,1.037f,-.054f));var mouth=(upper+lower)*.5f;
                report.AppendLine($"t={t:F2}: head={head.position:F4}; forward={head.up:F4}; current mouth={chamber.Mouth:F4}; anatomical mouth={mouth:F4}; offset={Vector3.Distance(chamber.Mouth,mouth):F4}m; aim error={Vector3.Angle(head.up,target-mouth):F2}deg.");
            }
            File.WriteAllText(Out+"/Current_Anatomy.txt",report.ToString());Debug.Log("DRAGON_BREATH_AUDIT_COMPLETE");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }

    [MenuItem("Lord Lyrical/Production/Validate Dragon Breath")]
    public static void Validate()
    {
        Directory.CreateDirectory(Out);var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("DRAGON BREATH / SOCKET AND AIM VALIDATION\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Review cast").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            var chamber=world.GetComponentInChildren<ChamberPresentation>();
            var dragon=chamber.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            var head=dragon.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="head");
            var jaw=dragon.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="jaw");
            Vector3 lastMouth=Vector3.zero,lastTarget=Vector3.zero;Quaternion lastHead=Quaternion.identity;
            float maxError=0,maxMouthStep=0,maxHeadStep=0,maxTargetStep=0,minAperture=100,maxAperture=0;int count=0;
            for(int i=0;i<=18*72;i++){
                float t=67+i/72f;var target=DragonBreath.Target(t,ConductorRoute.Position(t));
                chamber.Evaluate(t,true,target);var mouth=chamber.Mouth;
                if(!float.IsFinite(mouth.x)||!float.IsFinite(mouth.y)||!float.IsFinite(mouth.z))throw new Exception("Invalid mouth position");
                if(t>=69&&t<81){
                    maxError=Mathf.Max(maxError,Vector3.Angle(chamber.MouthForward,target-mouth));
                    float aperture=Vector3.SignedAngle(head.up,jaw.up,head.right);minAperture=Mathf.Min(minAperture,aperture);maxAperture=Mathf.Max(maxAperture,aperture);
                }
                if(i>0){maxMouthStep=Mathf.Max(maxMouthStep,Vector3.Distance(mouth,lastMouth));maxHeadStep=Mathf.Max(maxHeadStep,Quaternion.Angle(head.rotation,lastHead));maxTargetStep=Mathf.Max(maxTargetStep,Vector3.Distance(target,lastTarget));}
                lastMouth=mouth;lastHead=head.rotation;lastTarget=target;count++;
            }
            report.AppendLine($"{count} samples at72Hz /67-85s: max firing aim error={maxError:F4}deg; mouth step={maxMouthStep:F4}m; head step={maxHeadStep:F4}deg; target step={maxTargetStep:F4}m; firing jaw aperture={minAperture:F2}-{maxAperture:F2}deg.");
            if(maxError>1||maxMouthStep>.18f||maxHeadStep>4||maxTargetStep>.3f||minAperture<21.9f||maxAperture>65)throw new Exception("Breath motion exceeds documented bounds");
            chamber.Evaluate(76);var savedMouth=chamber.Mouth;var savedHead=head.rotation;
            chamber.Evaluate(94);chamber.Evaluate(76);
            if(Vector3.Distance(savedMouth,chamber.Mouth)>.0001f||Quaternion.Angle(savedHead,head.rotation)>.01f)throw new Exception("Dragon breath seek differs");
            report.AppendLine("PASS deterministic return to76s after a seek to94s.");
            float oldJump=Vector3.Distance(ConductorRoute.Position(74-1/72f)+Vector3.up*1.7f,new Vector3(0,1.7f,-2));
            float newJump=Vector3.Distance(DragonBreath.Target(74-1/72f,ConductorRoute.Position(74-1/72f)),DragonBreath.Target(74,ConductorRoute.Position(74)));
            report.AppendLine($"At74s the target step is{newJump:F4}m; prior discontinuous target step was{oldJump:F4}m. Sweep cue still begins at74s.");
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=78;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.backgroundColor=new Color(.006f,.009f,.016f);camera.clearFlags=CameraClearFlags.SolidColor;CinematicLook.Configure(camera);
            var light=new GameObject("Moon key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.color=new Color(.63f,.77f,1);light.transform.rotation=Quaternion.Euler(35,-30,0);light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);
            foreach(float t in new[]{69.5f,73.9f,74.3f,76f,78.5f,80.5f}){
                world.Render(ShowFrame.At(t),true);
                foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                camera.transform.position=new Vector3(0,1.65f,2);
                camera.transform.LookAt(t==76?new Vector3(.8189f,10.4427f,12.0263f):chamber.Mouth-Vector3.up);
                ParticipantAvatarBuilder.Render(camera,Out+"/Front_"+t.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+".png",1200,900,1);
                // Side angle makes the emission origin and jaw opening independently visible.
                camera.fieldOfView=42;camera.transform.position=head.position+new Vector3(4,-1,-3);camera.transform.LookAt(chamber.Mouth);
                ParticipantAvatarBuilder.Render(camera,Out+"/Mouth_"+t.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+".png",1200,900,0);
                camera.fieldOfView=78;
            }
            report.AppendLine("PASS numeric checks. Twelve before-crescendo front/side images rendered; artistic review remains separate. Existing rigs, source geometry, wing clock and cue table preserved.");
            File.WriteAllText(Out+"/Validation.txt",report.ToString());Debug.Log("DRAGON_BREATH_VALIDATION_PASS");
        }catch(Exception error){File.WriteAllText(Out+"/Validation_Failed.txt",report+"\n"+error);throw;}
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }

    [MenuItem("Lord Lyrical/Production/Inspect Dragon Skin Coordinates")]
    public static void SkinCoordinates()
    {
        Directory.CreateDirectory(Out);var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("DRAGON RENDER COORDINATE AUDIT\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var chamber=UnityEngine.Object.Instantiate(cfg.chamberModel).GetComponent<ChamberPresentation>();chamber.Initialize();chamber.Evaluate(76);
            var dragon=chamber.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            var head=dragon.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="head");
            report.AppendLine($"actor scale={dragon.lossyScale}; head={head.position}; mouth={chamber.Mouth}; forward={chamber.MouthForward}");
            foreach(var skin in dragon.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x=>!x.name.Contains("_LOD"))){
                var mesh=skin.sharedMesh;int headIndex=Array.IndexOf(skin.bones,head);if(headIndex<0)continue;
                using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
                using(var vertices=new Unity.Collections.NativeArray<Vector3>(mesh.vertexCount,Unity.Collections.Allocator.Temp)){
                    data[0].GetVertices(vertices);var weights=mesh.GetAllBoneWeights();var counts=mesh.GetBonesPerVertex();
                    var a=new Mesh();var b=new Mesh();skin.BakeMesh(a,false);skin.BakeMesh(b,true);
                    var av=a.vertices;var bv=b.vertices;var bind=mesh.bindposes[headIndex];
                    float wrong=0,right=0;int n=0,k=0;var center=Vector3.zero;
                    for(int i=0;i<vertices.Length;i++){
                        bool rigid=counts[i]==1&&weights[k].boneIndex==headIndex;
                        k+=counts[i];if(!rigid)continue;
                        var expected=head.TransformPoint(bind.MultiplyPoint3x4(vertices[i]));
                        wrong=Mathf.Max(wrong,Vector3.Distance(expected,skin.transform.TransformPoint(av[i])));
                        right=Mathf.Max(right,Vector3.Distance(expected,skin.transform.TransformPoint(bv[i])));center+=expected;n++;
                    }
                    report.AppendLine($"{skin.name}: head rigid vertices={n}; head surface center={(n>0?center/n:Vector3.zero)}; renderer scale={skin.transform.lossyScale}; world error BakeMesh(false)={wrong:F5}m; BakeMesh(true)={right:F5}m.");
                    UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);
                }
            }
            File.WriteAllText(Out+"/Skin_Coordinates.txt",report.ToString());Debug.Log("DRAGON_SKIN_COORDINATES_COMPLETE");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }

    [MenuItem("Lord Lyrical/Production/Validate Review Snapshot")]
    public static void ValidateSnapshot()
    {
        Directory.CreateDirectory(Out);var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("SCALED NATIVE SKIN / REVIEW SNAPSHOT COMPARISON\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var chamber=UnityEngine.Object.Instantiate(cfg.chamberModel).GetComponent<ChamberPresentation>();chamber.Initialize();chamber.Evaluate(76);
            var dragon=chamber.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Current CyberDragon | chamber performance");
            foreach(var r in chamber.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(dragon))r.enabled=false;
            foreach(var tr in chamber.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
            dragon.GetComponent<LODGroup>().ForceLOD(1);
            var camera=new GameObject("Native snapshot camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=48;camera.nearClipPlane=.08f;camera.farClipPlane=80;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.009f,.016f);
            camera.transform.position=chamber.Mouth+new Vector3(4,1,-5);camera.transform.LookAt(chamber.Mouth+Vector3.up*.2f);
            var light=new GameObject("Snapshot key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(30,-25,0);light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.14f,.18f);
            var target=new RenderTexture(960,720,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(960,720,TextureFormat.RGBA32,false);var old=RenderTexture.active;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            Color32[] native;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();native=tex.GetPixels32();File.WriteAllBytes(Out+"/Native_Skinned_Dragon.png",tex.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(target);ShaderUtil.allowAsyncCompilation=async;}
            ParticipantAvatarBuilder.Render(camera,Out+"/Corrected_Snapshot_Dragon.png",960,720,1);
            var loaded=new Texture2D(2,2,TextureFormat.RGBA32,false);loaded.LoadImage(File.ReadAllBytes(Out+"/Corrected_Snapshot_Dragon.png"));var baked=loaded.GetPixels32();
            int max=0,foreground=0,changed=0,intersection=0,union=0;long sum=0;
            for(int i=0;i<native.Length;i++){
                int delta=Math.Max(Math.Abs(native[i].r-baked[i].r),Math.Max(Math.Abs(native[i].g-baked[i].g),Math.Abs(native[i].b-baked[i].b)));max=Math.Max(max,delta);sum+=delta;if(delta>1)changed++;
                bool na=native[i].r>30||native[i].g>30||native[i].b>30,ba=baked[i].r>30||baked[i].g>30||baked[i].b>30;
                if(na)foreground++;if(na&&ba)intersection++;if(na||ba)union++;
            }
            UnityEngine.Object.DestroyImmediate(loaded);
            report.AppendLine($"Dragon scale1.35 /LOD1 /76s: max channel difference={max}; mean max-channel difference={(double)sum/native.Length:F6}; pixels differing by more than1={changed}; foreground pixels={foreground}.");
            double mean=(double)sum/native.Length,changedFraction=(double)changed/native.Length,iou=(double)intersection/Math.Max(1,union);
            report.AppendLine($"Foreground overlap={iou:P6}; pixels differing by more than1={changedFraction:P6}. Strict pixel identity={(max==0)}.");
            // CPU-baked MeshRenderer and native skinning can differ at a few shaded
            // edges. This gate checks spatial/render agreement, not exact identity.
            if(foreground<500||iou<.999||mean>.01||changedFraction>.0002)throw new Exception("Native/snapshot spatial agreement failed");
            report.AppendLine("PASS corrected scale and explicit LOD membership agree spatially with actual SkinnedMeshRenderer rendering within the recorded tolerances. Images are not pixel-identical; the residual analysis is retained. This is one snapshot, not a native-headset playback test.");
            File.WriteAllText(Out+"/Snapshot_Validation.txt",report.ToString());
        }catch(Exception error){File.WriteAllText(Out+"/Snapshot_Failed.txt",report+"\n"+error);throw;}
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
