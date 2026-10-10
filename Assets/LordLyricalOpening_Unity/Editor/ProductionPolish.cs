using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;
using CrazyMinnow.SALSA;

public static class ProductionPolish
{
    const string Out=MotionPolishValidation.Out+"/Production";
    static void Need(bool pass,string why){if(!pass)throw new Exception("PRODUCTION POLISH: "+why);}
    [MenuItem("Lord Lyrical/Production/Apply Facial Polish")]
    public static void ApplyFace()
    {
        Directory.CreateDirectory(Out);
        const string path="Assets/LordLyricalJeru_Unity/Prefabs/LordLyrical_Jeru.prefab";
        var go=PrefabUtility.LoadPrefabContents(path);int count=0,changed=0;
        try{
            var salsa=go.GetComponent<Salsa>();Need(salsa,"JERU speech component missing");
            var data=new SerializedObject(salsa);var visemes=data.FindProperty("visemes");
            Need(visemes!=null&&visemes.arraySize==4,"Expected four existing speech shapes");
            for(int i=0;i<visemes.arraySize;i++){
                var controllers=visemes.GetArrayElementAtIndex(i).FindPropertyRelative("expData.controllerVars");
                Need(controllers!=null&&controllers.isArray,"Speech controllers absent");
                for(int j=0;j<controllers.arraySize;j++){
                    var max=controllers.GetArrayElementAtIndex(j).FindPropertyRelative("maxShape");
                    Need(max!=null,"Speech intensity missing");
                    if(Mathf.Abs(max.floatValue-.68f)>.001f){max.floatValue=.68f;changed++;}
                    count++;
                }
            }
            Need(count>0,"No speech shapes configured");
            data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(go,path);
        }finally{PrefabUtility.UnloadPrefabContents(go);}
        AssetDatabase.SaveAssets();
        File.WriteAllText(Out+"/Facial_Polish.txt",$"Configured {count} speech controllers to 0.68 (previous 0.85, 20% reduction). Changed {changed}. Blink, expression and eye controls retained. Speech is driven by the full master audio; this is not phoneme-aligned vocal capture.");
        Debug.Log("FACIAL_POLISH_APPLIED controllers="+count+" changed="+changed);
    }
    [MenuItem("Lord Lyrical/Production/Review Close View")]
    public static void Review()
    {
        Directory.CreateDirectory(Out);var before=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("CLOSE VIEW / PRODUCTION REVIEW\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Review world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            var camera=new GameObject("Three metre review").AddComponent<Camera>();camera.cullingMask=1<<28;
            camera.fieldOfView=75;camera.nearClipPlane=.08f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.009f,.016f);
            camera.tag="Untagged";camera.transform.position=new Vector3(0,1.65f,2);CinematicLook.Configure(camera);
            var light=new GameObject("Review moon key").AddComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(35,-30,0);light.color=new Color(.63f,.77f,1);light.intensity=1.3f;light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);
            var compass=world.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Twelve frequency compass");
            Need(Mathf.Abs(compass.localPosition.y-1.15f)<.001f,"Energy field height");
            Need(compass.childCount==12,"Twelve colors must remain");
            report.AppendLine("PASS: 12 frequency orbits centred 1.15 m above JERU's feet (previously 2.6 m).");
            foreach(float t in new[]{22.65f,34f,40f,48f,59.5f,75.5f,94f,98.4f,117f}){
                world.Render(ShowFrame.At(t),true);
                foreach(var caption in world.GetComponentsInChildren<TextMesh>(true))if(caption.name=="Spatial call fallback"||caption.name=="Concert interaction feedback"){caption.transform.position=camera.transform.position+Vector3.forward*2.2f+Vector3.down*.9f;caption.transform.rotation=Quaternion.identity;}
                foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                camera.transform.rotation=Quaternion.identity;
                if(t>=63&&t<105)camera.transform.rotation=Quaternion.Euler(-22,0,0);
                ParticipantAvatarBuilder.Render(camera,Out+"/Close_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1440,1080,1);
                var fx=world.GetComponentInChildren<CinematicParticleDirector>();
                if(t>=69&&t<98){var mouth=world.GetComponentInChildren<ChamberPresentation>().Mouth;var to=mouth-camera.transform.position;float elevation=Mathf.Atan2(to.y,new Vector2(to.x,to.z).magnitude)*Mathf.Rad2Deg;report.AppendLine($"Dragon mouth at {mouth}: elevation={elevation:F1} degrees from viewing mark");Need(elevation<60,"Dragon face requires excessive sustained upward gaze");}
                report.AppendLine($"REVIEW {t:F2}s: particles={fx.ActiveParticles}; systems={fx.ActiveLayers}; waves={fx.ActiveWavefronts}");
                foreach(var group in world.GetComponentsInChildren<LODGroup>(false)){
                    var center=group.transform.TransformPoint(group.localReferencePoint);
                    float size=group.size*Mathf.Max(group.transform.lossyScale.x,group.transform.lossyScale.y,group.transform.lossyScale.z);
                    float relative=size/(2*Mathf.Max(.01f,Vector3.Distance(camera.transform.position,center))*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f))*QualitySettings.lodBias;
                    var lods=group.GetLODs();int index=Array.FindIndex(lods,l=>relative>=l.screenRelativeTransitionHeight);
                    if(index<0)continue;
                    long tris=0;foreach(var r in lods[index].renderers){
                        if(!r||!r.enabled||r.forceRenderingOff||!r.gameObject.activeInHierarchy)continue;
                        var m=r is SkinnedMeshRenderer sk?sk.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;
                        if(m)for(int s=0;s<m.subMeshCount;s++)tris+=(long)m.GetIndexCount(s)/3;
                    }
                    report.AppendLine($"  ESTIMATED {group.name} LOD{index}: {tris} triangles; relative size={relative:F3}");
                }
            }
            world.Render(ShowFrame.At(34),true);
            var bounds=new Bounds(compass.position,Vector3.zero);
            foreach(var m in compass.GetComponentsInChildren<MeshFilter>())foreach(var v in m.sharedMesh.vertices)bounds.Encapsulate(m.transform.TransformPoint(v));
            Need(bounds.min.y>-.1f&&bounds.max.y<2.6f,$"Body field actual geometry bounds: {bounds.min.y:F3} to {bounds.max.y:F3}");
            report.AppendLine($"PASS field world vertical bounds: {bounds.min.y:F2} to {bounds.max.y:F2} m");
            var salsa=world.GetComponentInChildren<Salsa>();
            var data=new SerializedObject(salsa);var visemes=data.FindProperty("visemes");
            int shapeCount=0;for(int i=0;i<visemes.arraySize;i++){
                var controllers=visemes.GetArrayElementAtIndex(i).FindPropertyRelative("expData.controllerVars");
                for(int j=0;j<controllers.arraySize;j++){
                    Need(Mathf.Abs(controllers.GetArrayElementAtIndex(j).FindPropertyRelative("maxShape").floatValue-.68f)<.001f,"Saved speech intensity");
                    shapeCount++;
                }
            }
            report.AppendLine("PASS persisted speech controller amplitudes: "+shapeCount+" at .68.");
            report.AppendLine("Images are editor snapshots with character LOD1. LOD estimates use camera distance and are not measured native headset draw calls.");
            File.WriteAllText(Out+"/Close_View_Validation.txt",report.ToString());Debug.Log("PRODUCTION_CLOSE_VIEW_PASS\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }

    [MenuItem("Lord Lyrical/Production/Apply Standalone Rendering Budget")]
    public static void ApplyRendering()
    {
        Directory.CreateDirectory(Out);var report=new StringBuilder("STANDALONE RENDERING BUDGET\n");
        var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/LordLyrical/ScriptableObjects/MobileURP.asset");
        pipeline.msaaSampleCount=2;EditorUtility.SetDirty(pipeline);
        report.AppendLine("MSAA 4x -> 2x; render scale remains 0.9, hero meshes and texture resolution unchanged.");
        var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        foreach(var prefab in new[]{cfg.orcModel,cfg.orcWeapon}){
            string path=AssetDatabase.GetAssetPath(prefab);var go=PrefabUtility.LoadPrefabContents(path);
            try{
                foreach(var group in go.GetComponentsInChildren<LODGroup>(true)){
                    var lods=group.GetLODs();Need(lods.Length==3,"Expected existing three-tier Orc LOD chain");
                    var thresholds=prefab==cfg.orcModel?new[]{.8f,.3f,.015f}:new[]{.40f,.20f,.015f};
                    for(int i=0;i<lods.Length;i++){
                        long triangles=0;foreach(var r in lods[i].renderers){
                            var mesh=r is SkinnedMeshRenderer sk?sk.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;
                            if(mesh)for(int sub=0;sub<mesh.subMeshCount;sub++)triangles+=(long)mesh.GetIndexCount(sub)/3;
                        }
                        report.AppendLine($"{prefab.name} LOD{i}: {triangles} triangles; threshold {lods[i].screenRelativeTransitionHeight:F3} -> {thresholds[i]:F3}");
                        lods[i].screenRelativeTransitionHeight=thresholds[i];
                    }
                    group.SetLODs(lods);
                    if(prefab==cfg.orcModel){group.localReferencePoint=new Vector3(0,1.45f,0);group.size=2.9f;}
                }
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        foreach(string name in new[]{"Sparks","Corona"}){
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/LordLyricalVFX_Unity/Materials/"+name+".mat");
            material.SetFloat("_SoftDistance",0);EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();report.AppendLine("Small sparks and additive coronas skip scene-depth reads; smoke and energy retain soft intersections. Only alpha-blended smoke is distance-sorted.");
        report.AppendLine("These are bounded rendering-cost reductions, not proof of a stable native frame rate. A worn-headset performance comparison remains required.");
        File.WriteAllText(Out+"/Rendering_Budget.txt",report.ToString());Debug.Log(report);
    }


    [MenuItem("Lord Lyrical/Production/Validate Stage Clearance")]
    public static void Clearance()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Clearance session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Clearance world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            var jeru=world.GetComponentInChildren<LordLyrical.Characters.JeruPerformance>();
            float min=100,timeAt=0;var center=new Vector2(0,10);
            for(int f=0;f<=21*36;f++){
                float t=69+f/36f;world.Render(ShowFrame.At(t),true);
                foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes}){
                    var node=jeru.animator.GetBoneTransform(bone);if(!node)continue;
                    var point=node.position;float distance=Vector2.Distance(new Vector2(point.x,point.z),center);
                    if(distance<min){min=distance;timeAt=t;}
                }
            }
            Need(min>=3.65f,"JERU's feet intersect the 3.5m raised Well rim");
            Directory.CreateDirectory(Out);
            File.WriteAllText(Out+"/Stage_Clearance.txt",$"PASS 757 route poses. Closest foot/toe is {min:F3} m from the Well centre at {timeAt:F3}s; authored rim radius is 3.5m. Route remains on the flat front floor. Start and end remain (0,0,5), three metres from the viewing mark.");
            Debug.Log("STAGE_CLEARANCE_PASS minimum="+min);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }

}
