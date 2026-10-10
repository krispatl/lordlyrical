using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using LordLyrical;
public static class Hero27Review
{
    const string Out=MotionPolishValidation.Out+"/Production/Hero27";
    [MenuItem("Lord Lyrical/Hero27/Validate")]
    public static void Validate()
    {
        Directory.CreateDirectory(Out);
        for(int i=0;i<=121*72;i++){
            float t=i/72f,a=HeroDuelScore.Anticipation(t),e=HeroDuelScore.RightExtension(t);
            if(!float.IsFinite(a)||!float.IsFinite(e)||a<0||a>1||e<.3f||e>.90f)throw new Exception("Hero motion outside bounds");
            if(i>0&&Mathf.Abs(e-HeroDuelScore.RightExtension(t-1/72f))>.07f)throw new Exception("Hero arm continuity");
        }
        Performance25Review.EvidenceOverride=Out;
        try{Performance25Review.Performance();Performance25Review.Duel();Performance25Review.Beams();}
        finally{Performance25Review.EvidenceOverride=null;}
        File.WriteAllText(Out+"/Hero27_Validation.txt","PASS authored anticipation, bounded reach and continuity; JERU, duel aim, timed dragon destruction and beam regressions.\nNative headset/multiple physical participants still require a wearer check.");
        Debug.Log("HERO27_PASS");
    }
    [MenuItem("Lord Lyrical/Hero27/Prepare")]
    public static void Prepare(){
        PlayerSettings.bundleVersion="1.0.27";PlayerSettings.Android.bundleVersionCode=27;
        LordLyrical.Editor.ShowBuilder.ConfigureAndroid();AssetDatabase.SaveAssets();Debug.Log("HERO27_READY");
    }

    [MenuItem("Lord Lyrical/Hero27/Render Fight")]
    public static void RenderFight(){
        Directory.CreateDirectory(Out);
        var prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        try{
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Hero review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Hero review world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var cam=new GameObject("Hero review camera").AddComponent<Camera>();cam.transform.position=new Vector3(3.8f,2.8f,-.8f);cam.transform.LookAt(new Vector3(0,3,8));cam.fieldOfView=70;cam.nearClipPlane=.08f;cam.farClipPlane=100;cam.cullingMask=1<<28;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.032f,.046f);
            var key=new GameObject("Review key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.5f;key.transform.rotation=Quaternion.Euler(35,-25,0);key.cullingMask=1<<28;
            foreach(float t in new[]{71.4f,72.15f,76.5f,96.5f,98.2f,100.2f}){
                world.Render(ShowFrame.At(t),true);
                foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                ParticipantAvatarBuilder.Render(cam,Out+"/Fight_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1440,1000,1);
            }
            Debug.Log("HERO27_RENDER_PASS");
        }finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(prior);}
    }
}
