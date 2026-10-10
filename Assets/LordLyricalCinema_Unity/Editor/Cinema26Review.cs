using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.PXR;
using UnityEngine.XR;
using LordLyrical;

public sealed class CinemaTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/LordLyricalCinema_Unity/Resources/Cinema26/"))return;
        var i=(TextureImporter)assetImporter;i.textureType=TextureImporterType.Default;i.sRGBTexture=true;i.alphaSource=TextureImporterAlphaSource.FromInput;i.alphaIsTransparency=true;
        i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Trilinear;i.mipmapEnabled=true;i.maxTextureSize=2048;i.isReadable=false;
        var android=i.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_6x6;i.SetPlatformTextureSettings(android);
    }
}
public static class Cinema26Review
{
    const string Out=MotionPolishValidation.Out+"/Production/Cinema26";
    static void Need(bool value,string message){if(!value)throw new Exception("CINEMA26: "+message);}
    [MenuItem("Lord Lyrical/Cinema26/Prepare Assets")]
    public static void Prepare()
    {
        foreach(var id in new[]{"CH_Stone","CH_NicheStone","CH_Obsidian","CH_EngravedGlyph","CH_Titanium","CH_SilverInlay"}){
            var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/LordLyricalChamber_Unity/Materials/"+id+".mat");if(!m)continue;
            Color c=id=="CH_Stone"?new Color(.42f,.46f,.52f):id=="CH_NicheStone"?new Color(.15f,.19f,.25f):id=="CH_Obsidian"?new Color(.075f,.10f,.15f):id=="CH_EngravedGlyph"?new Color(.11f,.15f,.20f):id=="CH_Titanium"?new Color(.36f,.43f,.52f):new Color(.60f,.66f,.72f);
            m.SetColor("_BaseColor",c);EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
        foreach(var id in new[]{"LordLyricalTitle","ResonanceSigil"})AssetDatabase.ImportAsset("Assets/LordLyricalCinema_Unity/Resources/Cinema26/"+id+".png",ImportAssetOptions.ForceUpdate);
        PlayerSettings.bundleVersion="1.0.26";PlayerSettings.Android.bundleVersionCode=26;
        LordLyrical.Editor.ShowBuilder.ConfigureAndroid();AssetDatabase.SaveAssets();Directory.CreateDirectory(Out);
        Debug.Log("CINEMA26_ASSETS_READY");
    }
    [MenuItem("Lord Lyrical/Cinema26/Review Alignment UI")]
    public static void ReviewUI()
    {
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try {
            var camera=new GameObject("UI review").AddComponent<Camera>();camera.transform.position=Vector3.zero;camera.fieldOfView=82;camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.20f);
            var label=new GameObject("Instruction card");label.transform.position=new Vector3(0,-.32f,1.35f);
            var text=label.AddComponent<TextMesh>();text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.lineSpacing=1.15f;
            text.text="Stand on the shared floor mark\nFace the stage\nHold both fists to align  95%";
            var card=label.AddComponent<SpatialInstructionCard>();card.Initialize(text);card.Progress(.95f);
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;
            ParticipantAvatarBuilder.Render(camera,Out+"/Alignment_Card.png",1440,1440,1);
            var bounds=text.GetComponent<MeshRenderer>().bounds;
            File.WriteAllText(Out+"/Alignment_UI_Validation.txt","Text bounds: "+bounds.size+"; panel: 0.92 x 0.32 metres.\n");
            Need(bounds.size.x<.86f&&bounds.size.y<.25f,"Alignment copy overflows card: "+bounds.size);
            foreach(string words in new[]{"ALIGNED\nMove to your spot\nThe show will begin together","ALLOW HAND TRACKING\nUse your hands to enter the realm","ENABLE HAND TRACKING\nLord Lyrical · app permissions"}){
                text.text=words;ParticipantAvatarBuilder.Render(camera,Out+"/Alignment_State_Check.png",800,800,1);
                var b=text.GetComponent<MeshRenderer>().bounds;Need(b.size.x<.86f&&b.size.y<.25f,"Alignment state overflows: "+words);
            }
            File.AppendAllText(Out+"/Alignment_UI_Validation.txt","PASS all alignment and permission states fit within the panel.\n");
            Debug.Log("CINEMA26_UI_PASS");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);}
    }
    [MenuItem("Lord Lyrical/Cinema26/Run Regressions")]
    public static void Regressions()
    {
        Performance25Review.EvidenceOverride=Out+"/Regression";
        try{
            LordLyrical.Editor.ShowVerification.VerifyCore();
            Performance25Review.Performance();
            Performance25Review.Beams();
            Performance25Review.Duel();
            File.WriteAllText(Out+"/Regression_Pass.txt","PASS core music clock/cues, JERU full 72Hz score, magical palm ownership/occlusion/release, preserved dragon duel.\n");
            Debug.Log("CINEMA26_REGRESSION_PASS");
        }finally{Performance25Review.EvidenceOverride=null;}
    }
    [MenuItem("Lord Lyrical/Cinema26/Validate and Render")]
    public static void Review()
    {
        Directory.CreateDirectory(Out);var log=new StringBuilder("CINEMA26 VALIDATION\n");
        Need(OpeningTiming.Prelude==17&&OpeningTiming.VrComplete==9,"Opening music timing changed");
        Need(!OpeningTiming.UsesPassthrough(50)&&OpeningTiming.UsesPassthrough(114)&&OpeningTiming.UsesPassthrough(121),"Return passthrough lifecycle");
        Need(CinemaTiming.Realm(113)==0&&CinemaTiming.Realm(119)==1&&CinemaTiming.Founder(121)==1,"Release timing");
        Need(SpatialRoomSurvey.Valid(new[]{Vector3.zero,Vector3.right,Vector3.up},new ushort[]{0,1,2}),"Valid measured mesh");
        Need(!SpatialRoomSurvey.Valid(new[]{Vector3.zero,Vector3.right,Vector3.up},new ushort[]{0,1,3}),"Invalid mesh index accepted");
        Need(!SpatialRoomSurvey.Valid(new[]{new Vector3(float.NaN,0,0),Vector3.right,Vector3.up},new ushort[]{0,1,2}),"Nonfinite mesh accepted");
        var fight=new ConcertEncounter();fight.occupied[0]=fight.occupied[1]=true;
        Need(fight.RegisterContribution(0,0,30)==1&&fight.RegisterContribution(0,0,30.1f)==1,"Two hands falsely count as two participants");
        Need(Mathf.Abs(fight.RegisterContribution(0,1,30.2f)-1.12f)<.0001f&&fight.State.cooperativeBursts==1,"Distinct participant bonus");
        fight.occupied[0]=false;Need(fight.RegisterContribution(0,1,30.3f)==1,"Disconnected participant still contributes");
        fight.occupied[0]=true;Need(fight.RegisterContribution(0,1,31)==1,"Expired contribution still counts");
        fight.Reset(2,0);Need(fight.RegisterContribution(0,1,30)==1,"Prior run contribution leaked");
        log.AppendLine("PASS fixed music timing; passthrough return; mesh bounds/finite guards; cooperative damage, expiry, disconnect and reset.");
        var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        PassthroughOpening opening=null;bool oldAsync=ShaderUtil.allowAsyncCompilation;
        try{
            ShaderUtil.allowAsyncCompilation=false;
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Cinema26 offline review").AddComponent<NetworkSessionController>();
            var world=new GameObject("Review world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var origin=new GameObject("Synthetic room tracking origin").transform;
            var cam=new GameObject("Cinema26 review camera").AddComponent<Camera>();cam.transform.SetParent(origin,false);
            cam.transform.position=new Vector3(0,1.68f,2);cam.transform.rotation=Quaternion.Euler(-3,0,0);cam.nearClipPlane=.08f;cam.farClipPlane=100;cam.fieldOfView=82;cam.cullingMask=1<<28;
            cam.backgroundColor=new Color(.025f,.032f,.046f);cam.clearFlags=CameraClearFlags.SolidColor;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.2f,.23f,.28f);
            var key=new GameObject("Review key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.5f;key.transform.rotation=Quaternion.Euler(40,-25,0);key.cullingMask=1<<28;
            opening=new PassthroughOpening(world,config,cam);
            var survey=opening.Cinema.Survey;
            var apply=typeof(SpatialRoomSurvey).GetMethod("Apply",BindingFlags.Instance|BindingFlags.NonPublic);
            // Deliberately synthetic fixtures for geometry/registration tests, not headset room evidence.
            apply.Invoke(survey,new object[]{new PxrSpatialMeshInfo{uuid=Guid.NewGuid(),position=Vector3.zero,rotation=Quaternion.Euler(90,0,0),vertices=new[]{new Vector3(-6,-8,0),new Vector3(6,-8,0),new Vector3(-6,8,0),new Vector3(6,8,0)},indices=new ushort[]{0,2,1,1,2,3},state=MeshChangeState.Added}});
            apply.Invoke(survey,new object[]{new PxrSpatialMeshInfo{uuid=Guid.NewGuid(),position=new Vector3(0,2.5f,7),rotation=Quaternion.identity,vertices=new[]{new Vector3(-6,-2.5f,0),new Vector3(6,-2.5f,0),new Vector3(-6,2.5f,0),new Vector3(6,2.5f,0)},indices=new ushort[]{0,2,1,1,2,3},state=MeshChangeState.Added}});
            Need(survey.SurfaceCount==2&&survey.VertexCount==8,"Room overlay patch accounting");
            survey.TrySurfacePoint(0,out var before);origin.position+=Vector3.right*.7f;survey.TrySurfacePoint(0,out var after);
            Need(Vector3.Distance(after-before,Vector3.right*.7f)<.001f,"Measured room failed to follow shared alignment origin");origin.position-=Vector3.right*.7f;
            foreach(float t in new[]{-17f,-.55f,2.5f,7.5f,24f,67f,96f,114.8f,116.5f,119f,121.1f}){
                world.Render(ShowFrame.At(Mathf.Clamp(t,0,121)),t>=0);opening.Render(t);
                foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                Need(opening.Cinema.ActiveParticles<=CinemaPresentation.ParticleBudget,"Transition particle budget exceeded");
                if(t>=113.5f)Need(opening.CameraBackgroundAlpha==0&&!opening.CameraHdr&&!opening.CameraPostProcessing,"Return fails projection alpha");
                ParticipantAvatarBuilder.Render(cam,Out+"/Cinema_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1440,1440,1);
            }
            world.Render(ShowFrame.At(121),true);opening.Render(121);
            Need(world.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeInHierarchy).All(r=>r.forceRenderingOff),"Realm or founder remains after return");
            Need(opening.Cinema.TitleOpacity>.99f,"Ending title missing");
            world.Render(ShowFrame.At(0),false);opening.Render(double.NegativeInfinity);
            Need(opening.Cinema.RealmRelease==0&&Shader.GetGlobalFloat("_RealmDissolve")==0,"Restart leaves dissolved world");
            Need(opening.Cinema.ActiveParticles==0&&opening.Cinema.TitleOpacity>0,"Restart leaks transition particles");
            Need(world.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Current CyberDragon | chamber performance")==1,"Preserved dragon duplicated");
            var inventory=world.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).GroupBy(m=>m.shader.name).Select(g=>g.Key+" : "+g.Count());
            File.WriteAllLines(Out+"/Shader_Inventory.txt",inventory);
            log.AppendLine("PASS measured-room alignment; all frame transitions; particle budget; title; final world concealment; backwards restart; exactly one current dragon.");
            log.AppendLine("Preview room planes are synthetic. Native spatial data, wearer visibility and frame rate require a headset run.");
            foreach(string name in new[]{"Title","RoomResonance","RealmPeel","InstructionCard"}){
                var shader=Resources.Load<Shader>("Cinema26/"+name);Need(shader&&shader.isSupported,"Unsupported shader "+name);
                foreach(var error in ShaderUtil.GetShaderMessages(shader))Need(error.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,name+": "+error.message);
            }
            log.AppendLine("PASS new shaders compile and support the editor render pipeline.");
            File.WriteAllText(Out+"/Cinema26_Validation.txt",log.ToString());Debug.Log("CINEMA26_PASS\n"+log);
        }finally{opening?.Dispose();EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prior);ShaderUtil.allowAsyncCompilation=oldAsync;}
    }
}