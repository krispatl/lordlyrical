using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Unity.XR.PXR;
using LordLyrical;
public static class Magic33Validation
{
    public static string OutputOverride;
    static void Need(bool value,string why){if(!value)throw new Exception("MAGIC33: "+why);}
    public static void Run()
    {
        string output=OutputOverride??Production33Review.Out;Directory.CreateDirectory(output);
        Need(OpeningTiming.Prelude==17&&OpeningTiming.Arrival==-2&&OpeningTiming.VrComplete==9,"Music or arrival timing changed");
        Need(CinemaTiming.IntroTitleGone==-5&&CinemaTiming.EndTitleStart>CinemaTiming.FounderGone&&CinemaTiming.FounderFadeStart>CinemaTiming.RealmGone,"Title/farewell timing changed");
        Need(Magic33Score.Energy(-17)==0&&Magic33Score.Energy(-2)==1&&Magic33Score.Energy(128)==0,"Hand transformation envelope");
        Need(Magic33Score.PortalOffering(0)==0&&Magic33Score.PortalOffering(9)==0&&!PalmBeamTiming.Allowed(9),"Portal offering unlocks beam early");
        Need(Magic33Score.Quiet(97.5f)<.31f&&Magic33Score.Quiet(98)==1,"Finale impact clock moved");
        Need(Magic33Score.SurfaceWave(21.2f,0)>Magic33Score.SurfaceWave(21.2f,10),"Glyphs fail to rise through architecture");
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;PassthroughOpening opening=null;
        try{
            var camera=new GameObject("Magic33 camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.17f,.19f);camera.nearClipPlane=.01f;camera.farClipPlane=100;
            foreach(bool left in new[]{true,false}){
                var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("LordLyrical/Tracked"+(left?"Left":"Right")+"Hand"));
                var hand=go.GetComponent<TrackedHandMesh>();var poses=hand.joints.Select(j=>new Pose(j.position,j.rotation)).ToArray();hand.ApplyJointPoses(poses,true);
                var fx=go.AddComponent<MagicalHandPresentation>();fx.Initialize(hand);
                var baked=new Mesh();hand.skin.BakeMesh(baked,true);var points=baked.vertices.Select(v=>hand.skin.transform.TransformPoint(v)).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
                camera.orthographic=true;camera.orthographicSize=.17f;camera.transform.position=bounds.center+new Vector3(0,.40f,-.30f);camera.transform.LookAt(bounds.center,Vector3.forward);
                var pose=new HandPoseInput.HandState{tracked=true,open=true,position=poses[0].position,rotation=poses[0].rotation,direction=Vector3.forward};
                foreach(float t in new[]{-17f,-2f,40f,126f,128f}){
                    Magic33Score.SetClock(t,t==-17);fx.Render(t,pose,new Color(.24f,.68f,1));
                    Need(fx.ActiveParticles<=MagicalHandPresentation.Budget,"Hand particle overflow");Need(hand.JointPositionError<.0001f,"Magic moved tracked skeleton");
                    Layer(scene);ParticipantAvatarBuilder.Render(camera,output+"/MagicHand_"+(left?"L":"R")+"_"+t+".png",900,700,1);
                }
                pose.closed=true;pose.open=false;fx.Render(40,pose,Color.cyan);Need(fx.ActiveParticles>0,"Closed palm has no gathered energy");
                pose.tracked=false;fx.Render(40,pose,Color.cyan);Need(fx.ActiveParticles==0,"Lost tracking leaves particles");
                UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(go);
            }
            camera.orthographic=false;camera.fieldOfView=82;camera.transform.position=new Vector3(0,1.68f,2);camera.transform.rotation=Quaternion.Euler(-3,0,0);
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("MR review session").AddComponent<NetworkSessionController>();var world=new GameObject("MR review world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            opening=new PassthroughOpening(world,cfg,camera);
            // With no scan, the landing area still has floor motes and the clock never waits.
            world.Render(ShowFrame.At(0),false);opening.Render(-.5f);Need(opening.Cinema.Survey.SurfaceCount==0&&opening.Cinema.ActiveParticles>0,"Missing-room fallback absent");
            Layer(scene);ParticipantAvatarBuilder.Render(camera,output+"/MR_FloorFallback.png",1200,900,1);
            var survey=opening.Cinema.Survey;var apply=typeof(SpatialRoomSurvey).GetMethod("Apply",BindingFlags.Instance|BindingFlags.NonPublic);
            apply.Invoke(survey,new object[]{new PxrSpatialMeshInfo{uuid=Guid.NewGuid(),position=Vector3.zero,rotation=Quaternion.Euler(90,0,0),vertices=new[]{new Vector3(-4,-8,0),new Vector3(4,-8,0),new Vector3(-4,8,0),new Vector3(4,8,0)},indices=new ushort[]{0,2,1,1,2,3},state=MeshChangeState.Added}});
            apply.Invoke(survey,new object[]{new PxrSpatialMeshInfo{uuid=Guid.NewGuid(),position=new Vector3(0,2.5f,7),rotation=Quaternion.identity,vertices=new[]{new Vector3(-4,-2.5f,0),new Vector3(4,-2.5f,0),new Vector3(-4,2.5f,0),new Vector3(4,2.5f,0)},indices=new ushort[]{0,2,1,1,2,3},state=MeshChangeState.Added}});
            Need(survey.SurfaceCount==2&&survey.VertexCount==8,"Room mesh accounting");
            int maximum=0;
            foreach(float t in new[]{-3f,-.5f,2.5f,7.5f,24f,97.5f,98.1f,114.8f,119f,122f,125.8f,127.5f}){
                world.Render(ShowFrame.At(Mathf.Clamp(t,0,121)),t>=0);opening.Render(t);maximum=Math.Max(maximum,opening.Cinema.ActiveParticles);
                Need(opening.Cinema.ActiveParticles<=CinemaPresentation.ParticleBudget,"Room particle overflow");Layer(scene);
                ParticipantAvatarBuilder.Render(camera,output+"/MR_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1200,900,1);
                if(t>=113.5f)Need(opening.CameraBackgroundAlpha==0&&!opening.CameraHdr,"Return alpha regressed");
                if(t==24)Need(survey.GetComponentsInChildren<MeshRenderer>().Length==0,"Room depth leaks into VR");
            }
            world.Render(ShowFrame.At(0),false);opening.Render(double.NegativeInfinity);Need(opening.Cinema.ActiveParticles==0&&Magic33Score.Clock==-1000&&Shader.GetGlobalFloat("_RealmDissolve")==0,"Replay retains effects");
            foreach(string name in new[]{"MagicHand","RoomOcclusion","RoomResonance","ContactMark"}){
                var shader=Resources.Load<Shader>("Cinema26/"+name);Need(shader&&shader.isSupported,"Unsupported shader "+name);
                Need(!ShaderUtil.GetShaderMessages(shader).Any(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader error "+name);
            }
            File.WriteAllText(output+"/Magic33_Validation.txt","PASS preserved opening/music and ordered farewell-before-title timing; hand envelopes; original tracked joints; tracking loss cleanup; bounded local/remote particles.\nPASS no-scan floor fallback, synthetic room/depth lifecycle, return alpha, replay cleanup and shader checks.\nRoom pool max sampled: "+maximum+" / "+CinemaPresentation.ParticleBudget+".\nImages use synthetic room fixtures and hand poses, not native sensor acceptance.\n");
        }finally{opening?.Dispose();Magic33Score.SetClock(0,true);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=async;}
        Debug.Log("MAGIC33_PASS");
    }
    static void Layer(Scene scene){foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;}
}
