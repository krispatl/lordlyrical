using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;

public static class ConcertBuilder
{
    const string Root="Assets/LordLyricalConcert_Unity";
    const string Stage="Assets/TirgamesAssets/PBRStageEquipment";
    const string Output=ConcertAudit.Output;
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Lord Lyrical/Concert/Build Stage and Navigation")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Resources/Concert");AssetDatabase.Refresh();
        string accentPath=Root+"/Resources/Concert/BeatAccents.mat";if(!AssetDatabase.LoadAssetAtPath<Material>(accentPath))AssetDatabase.CopyAsset("Assets/LordLyricalVFX_Unity/Materials/Sparks.mat",accentPath);
        var names=new[]{"DiscoLight01","Speakers01","Speakers03","Truss01","StageBase01"};var mats=new Dictionary<string,Material>();
        foreach(string name in names){
            string path=Root+"/Materials/"+name+"_URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.43f);
            foreach(var pair in new[]{("_BaseMap",""),("_BumpMap","_NM"),("_MetallicGlossMap","_Metallic"),("_OcclusionMap","_AO")}){
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Stage+"/Textures/"+name+pair.Item2+".png");if(!tex)continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                if(pair.Item1=="_BumpMap")importer.textureType=TextureImporterType.NormalMap;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});importer.SaveAndReimport();mat.SetTexture(pair.Item1,tex);
            }
            if(mat.GetTexture("_BumpMap"))mat.EnableKeyword("_NORMALMAP");if(mat.GetTexture("_MetallicGlossMap"))mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Metallic",.65f);EditorUtility.SetDirty(mat);mats[name]=mat;
        }
        var root=new GameObject("Concert stage equipment · owned Tirgames assets");
        try{
            for(int side=-1;side<=1;side+=2){
                Place("Speakers01",new Vector3(side*7,0,10),new Vector3(.95f,2.4f,.8f),mats["Speakers01"],root.transform);
                Place("Speakers03",new Vector3(side*6.3f,.1f,5.8f),new Vector3(1,.65f,.7f),mats["Speakers03"],root.transform);
                for(int z=0;z<3;z++)Place("DiscoLight01",new Vector3(side*8,.15f,4+z*4),new Vector3(.55f,.7f,.55f),mats["DiscoLight01"],root.transform);
                Place("Truss01_4m",new Vector3(side*9,8.2f,9),new Vector3(.3f,4,.3f),mats["Truss01"],root.transform);
            }
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Resources/Concert/StageRig.prefab");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
        var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.5f;settings.agentHeight=3;settings.agentClimb=.3f;settings.overrideVoxelSize=true;settings.voxelSize=.15f;
        var sources=new List<NavMeshBuildSource>{new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(new Vector3(0,-.1f,8),Quaternion.identity,Vector3.one),size=new Vector3(22,.2f,34),area=0}};
        // Exclude the performer's footprint and the physical speaker stacks from the walkable surface.
        foreach(var p in new[]{new Vector3(0,1.5f,5),new Vector3(-7,1.5f,10),new Vector3(7,1.5f,10)})sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(p,Quaternion.identity,Vector3.one),size=new Vector3(2.3f,3,2.3f),area=1});
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,2,8),new Vector3(24,8,36)),Vector3.zero,Quaternion.identity);Need(data,"Navigation build failed");
        string navPath=Root+"/Resources/Concert/ChamberNavigation.asset";var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);if(existing){EditorUtility.CopySerialized(data,existing);UnityEngine.Object.DestroyImmediate(data);}else AssetDatabase.CreateAsset(data,navPath);
        AssetDatabase.SaveAssets();Debug.Log("CONCERT_STAGE_NAVIGATION_READY");
    }
    static void Place(string name,Vector3 p,Vector3 size,Material material,Transform parent){
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Stage+"/Models/"+name+".FBX");Need(source,"Missing owned model "+name);
        var go=UnityEngine.Object.Instantiate(source,parent);go.name=name;var b=new Bounds();bool first=true;
        foreach(var r in go.GetComponentsInChildren<Renderer>()){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();}
        float scale=Mathf.Min(size.x/Mathf.Max(.01f,b.size.x),size.y/Mathf.Max(.01f,b.size.y),size.z/Mathf.Max(.01f,b.size.z));go.transform.localScale*=scale;go.transform.localPosition=p-new Vector3(b.center.x,b.min.y,b.center.z)*scale;
    }
    [MenuItem("Lord Lyrical/Concert/Verify and Render")]
    public static void Verify()
    {
        var report=new System.Text.StringBuilder("LORD LYRICAL CONCERT VALIDATION\n"+DateTime.Now+"\n");
        Need(!ConcertInteraction.IsClockJump(-.002f)&&ConcertInteraction.IsClockJump(-.4f)&&ConcertInteraction.IsClockJump(1),"Clock-jitter regression");
        new MusicReactiveSystem();var score=MusicReactiveSystem.Score;Need(score.beats.Length>200,"No score");Need(score.duration>121,"Score length");
        var musical=new MusicReactiveSystem();int beats=0;musical.OnBeat+=_=>beats++;for(int i=0;i<121*72;i++)musical.Evaluate(i/72f,true);Need(beats>200,"Events did not run");report.AppendLine("PASS song analysis and "+beats+" transient events; event-safe seek/reset");
        musical.Evaluate(20,true);int before=beats;musical.Evaluate(90,true);Need(beats==before,"Seek emitted stale beats");
        var encounter=new ConcertEncounter();encounter.occupied[0]=true;encounter.heads[0]=new Vector3(0,1.7f,0);for(int i=0;i<12;i++)encounter.State.orcs[i].position=new Vector3((i-6)*.5f,0,12+i*.2f);
        for(int i=0;i<800;i++)encounter.Tick(26+i/60f,1/60f);Need(encounter.State.orcs.Any(o=>o.position.z<5),"Orcs did not pursue");Need(encounter.State.hits>0,"No attack window reached");
        encounter.Reset(1,32);encounter.occupied[0]=true;encounter.heads[0]=new Vector3(0,1.7f,0);var a=encounter.State.orcs[0];a.position=new Vector3(0,0,1.9f);a.intent=OrcIntent.Telegraph;a.entered=32;encounter.guards[0]=34;encounter.Tick(32.9f,.016f);Need(encounter.State.blocks==1&&encounter.State.hits==0,"Guard failed");
        encounter.Reset(1,32);a=encounter.State.orcs[0];a.position=new Vector3(0,0,1.9f);a.intent=OrcIntent.Telegraph;a.entered=32;encounter.heads[0]=new Vector3(8,1.7f,0);encounter.Tick(32.9f,.016f);Need(encounter.State.hits==0,"Dodge ignored");
        encounter.heads[0]=new Vector3(0,1.7f,0);Need(encounter.Pulse(0,33,1),"Pulse rejected");Need(!encounter.Pulse(0,33.01f,1),"Cooldown bypassed");encounter.Pulse(0,33.4f,1);Need(a.health<=0,"Beat strike does not defeat");
        report.AppendLine("PASS pursuit, attack windows, guard, dodge, pulse damage, cooldown and defeat");
        encounter.Reset(1,32);for(int i=0;i<12;i++)if(i!=2)encounter.State.orcs[i].health=0;encounter.State.orcs[2].position=new Vector3(0,0,3);encounter.occupied[0]=true;encounter.heads[0]=new Vector3(0,1.7f,0);encounter.guards[0]=40;
        for(int i=0;i<240;i++)encounter.Tick(32+i/60f,1/60f);Need(encounter.State.blocks>0,"Flanking Orc never committed to an attack");report.AppendLine("PASS flanking unit closes distance and commits to a blockable strike");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");var session=new GameObject("concert test session").AddComponent<NetworkSessionController>();var world=new GameObject("concert world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var navigation=new NavMeshPath();Need(NavMesh.CalculatePath(new Vector3(0,0,12),Vector3.zero,NavMesh.AllAreas,navigation)&&navigation.status==NavMeshPathStatus.PathComplete&&navigation.corners.Length>=3,"Navigation did not route around JERU's stage");report.AppendLine("PASS baked navigation routes around the performer footprint");
            Need(world.GetComponentInChildren<JeruConcertFace>(),"Face integration absent");var salsa=world.GetComponentInChildren<CrazyMinnow.SALSA.Salsa>();Need(salsa&&salsa.visemes.Count==4,"SALSA not configured");
            var camera=new GameObject("Concert preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2.3f,-6.5f);camera.transform.rotation=Quaternion.Euler(-10,0,0);camera.fieldOfView=82;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.009f,.016f);camera.cullingMask=1<<28;CinematicLook.Configure(camera);
            var light=new GameObject("Concert review moon key").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-30,0);light.color=new Color(.63f,.77f,1);light.intensity=1.3f;light.cullingMask=1<<28;
            var oldAmbient=RenderSettings.ambientLight;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.1f,.12f,.16f);
            foreach(float t in new[]{0,21.5f,27f,39f,60,67,75,94,100,106,117,0,75}){
                world.Render(ShowFrame.At(t),true);var all=world.GetComponentsInChildren<Transform>(true);Need(!all.Any(x=>x.name.StartsWith("Sound Dragon -")),"Obsolete dragon returned");Need(all.Count(x=>x.name=="Current CyberDragon | chamber performance")==1,"Dragon ownership at "+t+": "+string.Join(" | ",all.Where(x=>x.name.Contains("Dragon")).Select(x=>x.name)));
                foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                if(t>0)ParticipantAvatarBuilder.Render(camera,Output+"/Concert_"+t.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+".png",1600,900,1);
            }
            RenderSettings.ambientLight=oldAmbient;
            Need(ShaderUtil.GetShaderMessages(Shader.Find("LordLyrical/ConcertScoreLight")).All(m=>m.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Score shader compile errors");
            report.AppendLine("PASS no duplicate CyberDragon across editor portal/reset/reverse seek; SALSA mapped; URP score shader; 11 rendered checkpoints");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        File.WriteAllText(Output+"/Concert_Validation.txt",report.ToString());Debug.Log("CONCERT_VALIDATED\n"+report);
    }
}
