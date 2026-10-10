using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;

public static class BeamRecoveryReview
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static void Need(bool ok,string why){if(!ok)throw new Exception("BEAM RECOVERY: "+why);}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Flags).SetValue(o,value);
    static object Get(object o,string field)=>o.GetType().GetField(field,Flags).GetValue(o);
    [MenuItem("Lord Lyrical/Production/Validate Beam Recovery")]
    public static void Run()
    {
        var report=new StringBuilder("CANDIDATE 19 / BEAM RECOVERY VALIDATION\n");
        var g=new ParticipantInputSequence();string a=new string('a',32),b=new string('b',32);
        Need(g.TryAccept(1,1,0,22,22,a,a,900),"Established sender rejected");
        Need(!g.TryAccept(1,1,0,22,22,a,a,899),"Out-of-order packet accepted");
        Need(!g.TryAccept(1,1,0,23,22,a,a,901),"Wrong participant accepted");
        Need(!g.TryAccept(1,1,0,22,22,b,a,1),"Unadvertised epoch accepted");
        Need(g.TryAccept(1,1,0,22,22,b,b,1),"Same-client reconnect sequence rejected");
        Need(!g.TryAccept(1,1,0,22,22,a,b,901),"Previous-session packet accepted");
        Need(g.TryAccept(1,1,0,24,24,a,a,1),"Replacement slot owner rejected");
        Need(!g.TryAccept(1,1,0,22,24,b,b,902),"Replaced owner accepted");
        Need(!g.TryAccept(2,1,0,24,24,a,a,3),"Previous run accepted");
        Need(g.TryAccept(2,2,0,24,24,a,a,1),"New run sequence rejected");
        Need(!g.TryAccept(2,2,10,24,24,a,a,2),"Invalid slot accepted");
        Need(!g.TryAccept(2,2,0,24,24,a,a,0),"Nonpositive sequence accepted");
        report.AppendLine("PASS ordering, owner validation, same-client reconnect, changed owner, obsolete epoch, run reset and malformed sequence.");
        var roster=new Roster{participants=new[]{new ParticipantRecord{slot=0,clientNo=22,connected=true},new ParticipantRecord{slot=1,clientNo=23,connected=false}}};
        Need(ParticipantInputSequence.OwnerOf(roster,0)==22,"Connected owner not found");
        Need(ParticipantInputSequence.OwnerOf(roster,1)==int.MinValue,"Disconnected owner accepted");
        report.AppendLine("PASS disconnected participants cannot own an active beam.");
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);Material targetMaterial=null,groundMaterial=null;
        string dir=MotionPolishValidation.Out+"/Production/BeamRecovery";Directory.CreateDirectory(dir);
        try{
            var holder=new GameObject("Staged beam validation");
            var session=holder.AddComponent<NetworkSessionController>();
            typeof(NetworkSessionController).GetProperty("Slot").SetValue(session,0);
            var encounter=new ConcertEncounter();
            for(int i=0;i<12;i++)encounter.State.orcs[i].position=new Vector3(50+i*3,0,8);
            encounter.State.orcs[0].position=new Vector3(0,0,6);
            var beam=holder.AddComponent<ManualResonanceBeams>();beam.Initialize(session,encounter,null);
            var all=(Array)Get(beam,"beams");var left=all.GetValue(0);var right=all.GetValue(1);
            Set(left,"active",true);Set(left,"origin",new Vector3(0,1.3f,.3f));Set(left,"direction",Vector3.forward);Set(left,"received",10f);
            Set(beam,"live",true);
            var draw=typeof(ManualResonanceBeams).GetMethod("Draw",Flags);
            draw.Invoke(beam,new object[]{27f,1/72f,10f});
            var core=(LineRenderer)Get(left,"core");
            Need(core.enabled&&beam.ActiveBeams==1,"Active beam renderer missing");
            Need(Vector3.Distance(core.GetPosition(15),new Vector3(0,1.3f,5.2f))<.001f,"Beam endpoint differs from aimed target");
            var particles=(ParticleSystem)Get(beam,"particles");
            Need(particles&&particles.particleCount>0&&particles.particleCount<=400,"Missing or unbounded beam particles");
            report.AppendLine("PASS live renderer endpoint reaches aimed Orc collision volume; finite particle pool populated.");
            draw.Invoke(beam,new object[]{27f,1/72f,10.36f});
            Need(!core.enabled&&beam.ActiveBeams==0,"Expired remote-equivalent sample stayed visible");
            Set(left,"active",true);Set(left,"received",10.4f);
            Set(right,"active",true);Set(right,"origin",new Vector3(.5f,1.3f,.3f));Set(right,"direction",Vector3.forward);Set(right,"received",10.4f);
            draw.Invoke(beam,new object[]{27f,1/72f,10.4f});
            ((float[])Get(beam,"held"))[0]=.3f;((float[])Get(beam,"held"))[1]=.3f;
            typeof(ManualResonanceBeams).GetMethod("OnApplicationFocus",Flags).Invoke(beam,new object[]{false});
            Need(!core.enabled&&!((LineRenderer)Get(right,"core")).enabled,"Focus loss left a beam renderer visible");
            Need(((float[])Get(beam,"held"))[0]==0&&((float[])Get(beam,"held"))[1]==0,"Focus loss retained acquisition dwell");
            Need(particles.particleCount==0,"Focus loss left impact particles");
            report.AppendLine("PASS timeout, immediate focus-loss cancellation for both hands, particle clearing and dwell reset.");
            ((float[])Get(beam,"charge"))[0]=.5f;
            ((Vector3[])Get(beam,"chargeOrigin"))[0]=new Vector3(0,1.3f,.3f);
            ((Vector3[])Get(beam,"chargeDirection"))[0]=Vector3.forward;
            draw.Invoke(beam,new object[]{27f,1/72f,10.5f});
            Need(beam.ActiveBeams==0&&particles.particleCount==4,"Acquisition charge missing or fired a beam early");
            typeof(ManualResonanceBeams).GetMethod("OnApplicationFocus",Flags).Invoke(beam,new object[]{false});
            Need(((float[])Get(beam,"charge"))[0]==0&&particles.particleCount==0,"Focus loss retained charge particles");
            report.AppendLine("PASS acquisition charge uses four particles without an active beam; focus loss clears it.");
            Set(left,"active",true);Set(left,"received",11f);Set(beam,"live",true);
            draw.Invoke(beam,new object[]{27f,1/72f,11f});
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary occlusion test";wall.transform.position=new Vector3(0,1.3f,3);wall.transform.localScale=new Vector3(1,2,.2f);
            Physics.SyncTransforms();draw.Invoke(beam,new object[]{27f,1/72f,11f});
            Need(Mathf.Abs(core.GetPosition(15).z-2.9f)<.001f,"Visual beam crossed wall");
            float health=encounter.State.orcs[0].health;
            Need(!encounter.Beam(0,27,1,new Vector3(0,1.3f,.3f),Vector3.forward)&&encounter.State.orcs[0].health==health,"Damage crossed wall");
            UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
            report.AppendLine("PASS wall occlusion agrees between beam rendering and combat damage.");
            draw.Invoke(beam,new object[]{27.2f,1/72f,11f});
            var target=GameObject.CreatePrimitive(PrimitiveType.Capsule);UnityEngine.Object.DestroyImmediate(target.GetComponent<Collider>());
            target.transform.position=new Vector3(0,1.1f,6);target.transform.localScale=new Vector3(.7f,1.1f,.7f);
            targetMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));targetMaterial.color=new Color(.16f,.22f,.28f);target.GetComponent<Renderer>().sharedMaterial=targetMaterial;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position=new Vector3(0,0,4);floor.transform.localScale=Vector3.one*2;
            groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.color=new Color(.025f,.035f,.05f);floor.GetComponent<Renderer>().sharedMaterial=groundMaterial;
            var lamp=new GameObject("Review light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=1.5f;lamp.transform.rotation=Quaternion.Euler(45,-30,0);
            var cam=new GameObject("Beam review camera").AddComponent<Camera>();cam.transform.position=new Vector3(3,2.2f,-1.5f);cam.transform.LookAt(new Vector3(0,1.25f,3.5f));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.013f,.017f,.025f);cam.fieldOfView=48;
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);rt.Create();var old=RenderTexture.active;
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);try{image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(dir+"/Beam_Render_Validation.png",image.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(image);}}
            finally{RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            report.AppendLine("STAGED RENDER: Beam_Render_Validation.png uses a simple test target, not real headset tracking.");
        }
        finally{
            EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);
            if(targetMaterial)UnityEngine.Object.DestroyImmediate(targetMaterial);if(groundMaterial)UnityEngine.Object.DestroyImmediate(groundMaterial);
        }
        File.WriteAllText(dir+"/Validation.txt",report.ToString());Debug.Log(report);
    }
}
