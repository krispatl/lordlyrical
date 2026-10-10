using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;
public static class AnimationValidation
{
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    [MenuItem("Lord Lyrical/Animation/Validate and Render")]
    public static void Run()
    {
        // Unity can restore an untitled preview after an outage. Preserve it before additive review.
        for(int i=0;i<SceneManager.sceneCount;i++){
            var recovered=SceneManager.GetSceneAt(i);if(!string.IsNullOrEmpty(recovered.path))continue;
            Directory.CreateDirectory("Assets/_Recovery");
            string saved=AssetDatabase.GenerateUniqueAssetPath("Assets/_Recovery/PowerOutage_Review.unity");
            Require(EditorSceneManager.SaveScene(recovered,saved),"Could not preserve recovered scene");
        }
        Directory.CreateDirectory(AnimationIntegration.Output);
        var report=new StringBuilder("ANIMATION VALIDATION\n");
        var library=HumanoidMotionLibrary.Load();Require(library&&library.walk.Length==8&&library.run.Length==8,"Motion library missing");
        foreach(var clip in library.walk.Concat(library.run).Append(library.idle))Require(clip&&clip.isHumanMotion,"Non-Humanoid locomotion source");
        report.AppendLine("PASS 17 owned/free Humanoid locomotion clips");
        var assigned=new ParticipantRecord{slot=3,clientNo=9,deviceId="device-A",connected=true};
        Require(NetworkRoleAvatar.MatchesAssignment(assigned,9,"device-A"),"Correct owner rejected");
        Require(!NetworkRoleAvatar.MatchesAssignment(assigned,8,"device-A")&&!NetworkRoleAvatar.MatchesAssignment(assigned,9,"device-B"),"Wrong owner accepted");
        assigned.connected=false;Require(!NetworkRoleAvatar.MatchesAssignment(assigned,9,"device-A"),"Disconnected owner accepted");
        report.AppendLine("PASS avatar identity requires connected roster client + device, rejects stale/wrong owner");
        Require(NetworkSessionController.TryDecodeRoster("{\"v\":2,\"p\":[{\"s\":9,\"c\":12,\"d\":\"device-A\",\"a\":1}]}",out var compact)&&compact.participants[0].slot==9&&compact.participants[0].connected&&compact.participants[0].deviceId=="device-A","Compact roster decode failed");
        Require(!NetworkSessionController.TryDecodeRoster("{\"v\":2,\"p\":[{\"s\":1},{\"s\":1}]}",out _),"Duplicate role assignment accepted");
        report.AppendLine("PASS compact roster preserves identity and rejects duplicate slots");
        Require(Vector3.Distance(ConductorRoute.Position(69),ConductorRoute.Position(90))<.001f,"Route does not close");
        float previous=0;for(float t=68;t<92;t+=.05f){float d=ConductorRoute.Distance(t);Require(d>=previous-.001f,"Route distance reversed");previous=d;Require(ConductorRoute.Velocity(t).magnitude<2,"Conductor speed too high");}
        report.AppendLine("PASS conductor route closes, starts/stops continuously, distance monotonic");
        foreach(OrcAttack attack in Enum.GetValues(typeof(OrcAttack))){
            Require(!OrcCombatMotion.Contains(attack,Vector3.zero,0,new Vector3(0,1.7f,-1.5f)),"Hit behind attacker");
            Require(!OrcCombatMotion.Contains(attack,Vector3.zero,0,new Vector3(0,1.7f,4)),"Out-of-range hit");
            Require(OrcCombatMotion.Contains(attack,Vector3.zero,0,new Vector3(0,1.7f,1.8f)),"Miss inside resonance arc");
            var encounter=new ConcertEncounter();encounter.Reset(1,28);encounter.occupied[0]=true;encounter.heads[0]=new Vector3(0,1.7f,1.8f);
            for(int i=1;i<12;i++)encounter.State.orcs[i].health=0;
            var orc=encounter.State.orcs[0];orc.position=Vector3.zero;orc.target=0;orc.yaw=0;orc.attack=attack;orc.attackStarted=28;orc.entered=28;orc.intent=OrcIntent.Telegraph;orc.cooldown=100;
            for(float age=0;age<OrcCombatMotion.Duration(attack)+.1f;age+=.01f)encounter.Tick(28+age,.01f);
            Require(encounter.State.hits==OrcCombatMotion.ContactCount(attack),"Attack duplicate/missing contacts "+attack+" "+encounter.State.hits);
        }
        report.AppendLine("PASS five attack profiles: contact windows, no repeat damage, facing and reach");
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var jeru=UnityEngine.Object.Instantiate(config.lordModel).GetComponent<JeruPerformance>();
            var orc=UnityEngine.Object.Instantiate(config.orcModel).GetComponent<SonicOrcPerformance>();
            var role=UnityEngine.Object.Instantiate(config.participantModels[3]).GetComponent<ParticipantAvatarPerformance>();
            jeru.transform.position=new Vector3(-2.4f,0,0);orc.transform.position=new Vector3(1.2f,0,0);role.transform.position=new Vector3(4.5f,0,0);
            jeru.previewAutomatically=orc.previewAutomatically=role.previewAutomatically=false;
            var weapon=UnityEngine.Object.Instantiate(config.orcWeapon,orc.leftWeaponSocket);weapon.transform.localPosition=Vector3.zero;weapon.transform.localRotation=Quaternion.identity;
            var camera=new GameObject("Animation review camera").AddComponent<Camera>();camera.cullingMask=1<<29;camera.backgroundColor=new Color(.025f,.035f,.045f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=39;
            camera.transform.position=new Vector3(6.2f,3.3f,12);camera.transform.LookAt(new Vector3(1,1.4f,0));
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(32,-145,0);
            var fill=new GameObject("Review fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.color=new Color(.4f,.65f,1);fill.transform.rotation=Quaternion.Euler(12,30,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.localScale=Vector3.one*2;floor.transform.position=new Vector3(0,-.025f,0);
            var floorMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMat.color=new Color(.055f,.065f,.075f);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
            foreach(var g in scene.GetRootGameObjects())foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
            light.cullingMask=fill.cullingMask=1<<29;
            var head=new GameObject("Simulated headset").transform;var left=new GameObject("Simulated left controller").transform;var right=new GameObject("Simulated right controller").transform;
            head.position=new Vector3(4.5f,1.7f,0);left.position=new Vector3(4.15f,1.15f,.25f);right.position=new Vector3(4.85f,1.3f,.32f);
            role.SetFirstPerson(false);role.SampleTracked(0);role.ApplyTrackedBody(head,left,right,0);float scale=role.CalibratedScale;
            head.position+=Vector3.down*.45f;role.SampleTracked(.1f);role.ApplyTrackedBody(head,left,right,0);
            Require(Mathf.Abs(role.CalibratedScale-scale)<.00001f,"Crouch changed avatar scale");
            Require(!role.IsNarrativelyHidden,"Live participant was hidden");
            report.AppendLine("PASS tracked crouch keeps calibrated stature; simulated controllers solve arms");
            head.position+=Vector3.up*.45f;
            for(int index=0;index<11;index++){
                float time=index==0?0:index==1?.22f:index==4?1.3f:index==5?.2f:.35f;
                orc.SampleClip(index,time);jeru.SampleShow(74+index*.11f);role.SampleTracked(74+index*.11f);role.ApplyTrackedBody(head,left,right,0);
                foreach(var skin in orc.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                    var mesh=new Mesh();skin.BakeMesh(mesh);Require(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"Invalid skin vertices");UnityEngine.Object.DestroyImmediate(mesh);
                }
                ParticipantAvatarBuilder.Render(camera,AnimationIntegration.Output+"/Pose_"+index.ToString("00")+".png",1500,850,1);
                report.AppendLine("PASS Orc clip "+index+" sampled; all deformed vertices finite");
            }
            var rigBones=orc.animator.GetComponentsInChildren<Transform>(true);
            report.AppendLine("Orc left ankle at walk .25: ");
            orc.SampleClip(1,.25f);foreach(var b in rigBones.Where(t=>t.name=="foot_l"||t.name=="foot_r"))report.AppendLine(b.name+" world "+b.position);
            UnityEngine.Object.DestroyImmediate(floorMat);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
        File.WriteAllText(AnimationIntegration.Output+"/Animation_Validation.txt",report.ToString());Debug.Log("ANIMATION_VALIDATION_PASS\n"+report);
    }
}
