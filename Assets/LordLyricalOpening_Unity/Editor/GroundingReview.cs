using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using LordLyrical.Characters;

public static class GroundingReview
{
    const string Out=MotionPolishValidation.Out+"/Production/Grounding";
    static void Need(bool yes,string reason){if(!yes)throw new Exception("GROUNDING: "+reason);}
    [MenuItem("Lord Lyrical/Production/Apply Grounded Presentation")]
    public static void Apply()
    {
        Directory.CreateDirectory(Out);
        var shader=Shader.Find("LordLyrical/Character Ground Contact");Need(shader,"Contact shader missing");
        const string matPath="Assets/Resources/Concert/CharacterGroundContact.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}else mat.shader=shader;
        EditorUtility.SetDirty(mat);
        const string path="Assets/LordLyricalOrc_Unity/Prefabs/Sonic_Orc.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var lod=root.GetComponent<LODGroup>();Need(lod&&lod.lodCount==3,"Existing three Orc detail levels required");
            var levels=lod.GetLODs();levels[0].screenRelativeTransitionHeight=.8f;levels[1].screenRelativeTransitionHeight=.3f;
            lod.SetLODs(levels);lod.localReferencePoint=new Vector3(0,1.45f,0);lod.size=2.9f;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("GROUNDED_PRESENTATION_APPLIED");
    }
    [MenuItem("Lord Lyrical/Production/Validate Grounded Presentation")]
    public static void Review()
    {
        Directory.CreateDirectory(Out);
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("GROUNDED PRESENTATION / CANDIDATE16 CORRECTED SNAPSHOT SCALE AND LOD\n");
        try{
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Review world").AddComponent<ResonantWorld>();world.Initialize(config,session);
            var contact=world.GetComponentInChildren<CharacterGroundContact>(true);Need(contact,"Ground contacts absent");
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.cullingMask=1<<28;
            camera.fieldOfView=65;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.009f,.016f);CinematicLook.Configure(camera);
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.color=new Color(.63f,.77f,1);light.transform.rotation=Quaternion.Euler(35,-30,0);light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);
            foreach(float time in new[]{0f,15f,22f,31f,47.8f,53.9f,54f,73f,98f,117f}){
                world.Render(ShowFrame.At(time),true);
                Need(contact.VisiblePatches<=39,"Unbounded contact geometry");
                var mesh=contact.GetComponent<MeshFilter>().sharedMesh;
                Need(mesh.vertexCount==contact.VisiblePatches*4,"Patch topology");
                foreach(var vertex in mesh.vertices)Need(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&Mathf.Abs(contact.transform.TransformPoint(vertex).y-.012f)<.0001f,"Invalid floor contact");
                if(time<21)Need(contact.VisiblePatches==0,"Contacts visible in vortex/opening");
                if(time>=54)Need(contact.VisiblePatches==3,"Inactive Orc contacts remained");
                report.AppendLine($"{time:F1}s: {contact.VisiblePatches} patches, {mesh.vertexCount} vertices, one renderer.");
            }
            world.Render(ShowFrame.At(34),true);var a=contact.GetComponent<MeshFilter>().sharedMesh.vertices;
            world.Render(ShowFrame.At(99),true);world.Render(ShowFrame.At(34),true);var b=contact.GetComponent<MeshFilter>().sharedMesh.vertices;
            Need(a.Length==b.Length&&a.Zip(b,(x,y)=>Vector3.Distance(x,y)).Max()<.0001f,"Contact seek mismatch");
            report.AppendLine("PASS floor placement, capacity, hidden phase cleanup and repeatable seek.");
            foreach(float time in new[]{34f,73f}){
                world.Render(ShowFrame.At(time),true);
                foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                var jeru=world.GetComponentInChildren<JeruPerformance>();camera.transform.position=jeru.transform.position+new Vector3(1.1f,1.25f,-2.6f);camera.transform.LookAt(jeru.transform.position+Vector3.up*.9f);
                foreach(bool enabled in new[]{false,true}){
                    contact.gameObject.SetActive(enabled);
                    ParticipantAvatarBuilder.Render(camera,Out+"/Contact_"+time.ToString("00")+"_"+(enabled?"after":"before")+".png",1440,1080,0);
                }
            }
            world.gameObject.SetActive(false);
            var actor=UnityEngine.Object.Instantiate(config.orcModel);actor.transform.position=Vector3.zero;actor.transform.rotation=Quaternion.Euler(0,180,0);
            foreach(var tr in actor.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
            var performance=actor.GetComponent<SonicOrcPerformance>();performance.SampleClip(2,.33f,0,true);performance.SetResonance(1.3f);
            camera.fieldOfView=90;
            foreach(float distance in new[]{3f,6f,12f}){
                camera.transform.position=new Vector3(0,1.65f,-distance);camera.transform.rotation=Quaternion.identity;
                float oldRelative=4.2f/(2*Vector3.Distance(camera.transform.position,new Vector3(0,1.45f,0)))*QualitySettings.lodBias;
                float newRelative=oldRelative*2.9f/4.2f;
                int oldLevel=oldRelative>=.42f?0:oldRelative>=.16f?1:2;
                int newLevel=newRelative>=.8f?0:newRelative>=.3f?1:2;
                ParticipantAvatarBuilder.Render(camera,Out+"/Orc_"+distance.ToString("00")+"m_before.png",1440,1440,oldLevel);
                ParticipantAvatarBuilder.Render(camera,Out+"/Orc_"+distance.ToString("00")+"m_after.png",1440,1440,newLevel);
                report.AppendLine($"{distance:F0}m, 90-degree view, LOD bias {QualitySettings.lodBias:F1}: old LOD{oldLevel}, new LOD{newLevel}. Existing source meshes and animated bounds unchanged.");
                if(distance==3)Need(oldLevel==newLevel&&newLevel==0,"Close opponent lost full detail");
            }
            var shader=Shader.Find("LordLyrical/Character Ground Contact");
            var errors=ShaderUtil.GetShaderMessages(shader).Where(x=>x.severity.ToString()=="Error").ToArray();Need(errors.Length==0,"Contact shader errors");
            report.AppendLine("PASS shader compilation. Source meshes, rigs, attack timing and tracked pose ownership unchanged.");
            File.WriteAllText(Out+"/Validation.txt",report.ToString());Debug.Log("GROUNDED_PRESENTATION_PASS");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
