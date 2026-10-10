using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace LordLyrical.Editor
{
    public static class ShowVerification
    {
        public static void VerifyAndRender(){VerifyCore();RenderFrames();}
        static void Assert(bool condition,string message){if(!condition)throw new Exception("LL TEST FAILED: "+message);}
        [MenuItem("Lord Lyrical/Tests/Verify Core")]
        public static void VerifyCore()
        {
            var results=new List<string>();
            Assert(RootKeys.Slots.Select(s=>s.key).Distinct().Count()==10,"Root Key uniqueness");results.Add("Ten unique Root Keys and typed role data");
            var table=JsonUtility.FromJson<CueTable>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/LordLyrical/Resources/Cues.json").text);
            var scheduler=new CueScheduler(table.cues);var fired=new List<string>();scheduler.Fired+=c=>fired.Add(c.id);
            for(double t=0;t<=121.02;t+=1d/72)scheduler.Advance(t);
            Assert(fired.Count==table.cues.Length,"All cues exactly once at 72Hz");Assert(fired.Distinct().Count()==fired.Count,"No duplicate cue fire");results.Add("Every authored cue fired once during full 72 Hz sequence");
            var retimed=JsonUtility.FromJson<CueTable>(JsonUtility.ToJson(table));retimed.cues.Single(c=>c.id=="resonance-shatter").time=40;var map=new CueTimeline(retimed.cues);Assert(Math.Abs(map.NarrativeTime(40)-39)<.000001&&Math.Abs(map.NarrativeTime(42)-42)<.000001,"Retime choreography from data");results.Add("Cue retiming maps continuous choreography without changing music time");
            scheduler.Reset();fired.Clear();scheduler.Seek(85);scheduler.Advance(85.01);Assert(fired.Count==0,"Late join must not replay historical bursts");scheduler.Advance(87);Assert(fired.SequenceEqual(new[]{"identify-discordant"}),"Next cue after late join");results.Add("85-second late join skips historical transients and preserves future cues");
            foreach(var t in new[]{0d,21,26,52,59,61,63,69,85,90,98,101,111,121}){var a=ShowFrame.At(t);var b=ShowFrame.At(t);Assert(a.time==b.time&&a.art==b.art&&a.dragonDissolve==b.dragonDissolve,"Pure reconstruction");}
            Assert(ShowFrame.At(59).wallActivation==1&&ShowFrame.At(61).wallActivation==0,"Exactly two seconds wall activation");Assert(ShowFrame.At(85).deepSonar&&ShowFrame.At(85).dragonReveal==1,"Dragon reconstruction");Assert(ShowFrame.At(121).titleProgress==1,"Final title state");results.Add("Environment reconstruction, exact wall window, Dragon and title final state");
            var dragonTestRoot=new GameObject("Dragon visibility test");var dragonTest=new DragonEncounterController(dragonTestRoot.transform);dragonTest.Render(ShowFrame.At(21));Assert(!dragonTest.root.gameObject.activeSelf,"Dragon concealed before entrance");dragonTest.Render(ShowFrame.At(63));Assert(dragonTest.root.gameObject.activeSelf,"Dragon entrance at 63 seconds");dragonTest.Render(ShowFrame.At(101));Assert(!dragonTest.root.gameObject.activeSelf,"Dragon gone after dissolution");UnityEngine.Object.DestroyImmediate(dragonTestRoot);
            var clock=new SynchronizedClock();for(int i=0;i<6;i++)clock.Sample(i,i+5.002,i+5.002,i+.004);Assert(clock.Ready&&Math.Abs(clock.Offset-5)<.000001,"Symmetric network clock");Assert(!clock.Sample(1,2,2,2),"Slow samples rejected");results.Add("NTP-style clock offset and outlier filtering");
            clock.Reset();Assert(!clock.Ready,"DSP restart invalidates synchronization");for(int i=0;i<3;i++)clock.Sample(i,i+20.002,i+20.002,i+.004);Assert(clock.Ready&&Math.Abs(clock.Offset-20)<.000001,"Reacquire offset after suspended DSP clock");results.Add("DSP suspend/restart clock reset and reacquisition");
            var show=new ShowSnapshot{bootId="test",phase="Running",mode="Adaptive",sdk="0.17.4",protocol=8,duration=121,openingDuration=OpeningTiming.Prelude,epoch=100,runId=2,revision=3};Assert(JsonUtility.FromJson<ShowSnapshot>(JsonUtility.ToJson(show)).IsValid,"State serialization");results.Add("Atomic show snapshot serialization");show.openingDuration=3;Assert(!show.IsValid,"Old prelude rejected");results.Add("Reject authority with an incompatible opening duration");
            bool duplicate=false;try{new CueScheduler(new[]{new Cue{id="same",time=0},new Cue{id="same",time=1}});}catch(ArgumentException){duplicate=true;}Assert(duplicate,"Duplicate cue rejection");results.Add("Duplicate cue IDs rejected");
            Directory.CreateDirectory("Evidence");File.WriteAllLines("Evidence/unity-core-tests.txt",results.Select(r=>"PASS "+r));Debug.Log("LL CORE TESTS PASS: "+results.Count+" groups");
        }
        [MenuItem("Lord Lyrical/Tests/Render Sequence Frames")]
        public static void RenderFrames()
        {
            ShaderUtil.allowAsyncCompilation=false;
            AssetDatabase.ImportAsset("Assets/LordLyrical/Art/Shaders/Resonance.shader",ImportAssetOptions.ForceUpdate);
            var shader=Shader.Find("LordLyrical/Resonance");
            Debug.Log($"LL shader supported={shader.isSupported} pipeline={GraphicsSettings.currentRenderPipeline}");
            foreach(var message in ShaderUtil.GetShaderMessages(shader))Debug.LogWarning(message.message);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Preview session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Preview world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2,-7);camera.transform.rotation=Quaternion.Euler(-6,0,0);camera.backgroundColor=new Color(.004f,.006f,.015f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=85;camera.farClipPlane=100;
            Directory.CreateDirectory("Evidence/Frames");var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;
            foreach(double t in new[]{0d,21,31,39,59,66,74,85,90,98.8,101,117,121})
            {world.Render(ShowFrame.At(t),true);RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes($"Evidence/Frames/show-{t:000.0}.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
            RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
            ShaderUtil.allowAsyncCompilation=true;
            EditorSceneManager.OpenScene("Assets/LordLyrical/Scenes/Experience.unity");Debug.Log("LL rendered thirteen sequence verification frames");
        }
        [MenuItem("Lord Lyrical/Tests/Capture Live Frame")]
        public static void CaptureLiveFrame(){Directory.CreateDirectory("Evidence");ScreenCapture.CaptureScreenshot("Evidence/live-frame.png");}
        [MenuItem("Lord Lyrical/Tests/Verify Live Remote Avatars")]
        public static void VerifyLiveRemoteAvatars()
        {
            var avatars=UnityEngine.Object.FindObjectsByType<Styly.NetSync.NetSyncAvatar>(FindObjectsSortMode.None);
            var remote=avatars.Where(a=>!a.IsLocalAvatar).ToArray();Assert(remote.Length==9,"Nine real network avatar instances");
            Assert(remote.All(a=>a.GetComponent<NetworkRoleAvatar>()!=null),"Keyed avatar presentation");
            Assert(remote.All(a=>a._head.position.y>1),"Remote tracked head poses received");
            Directory.CreateDirectory("Evidence");File.WriteAllLines("Evidence/unity-remote-avatars.txt",remote.Select(a=>$"PASS client={a.ClientNo} head={a._head.position} left={a._leftHand.position} right={a._rightHand.position}"));
            Debug.Log("LL REMOTE AVATARS PASS: nine posed client avatars beside local player");
        }
    }
}
