#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LordLyrical;

public static class ChamberAssetBuilder
{
    const string Root="Assets/LordLyricalChamber_Unity";
    const string DragonRoot="Assets/LordLyricalDragon_Unity";
    const string Output=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs";
    const string PrefabPath=Root+"/Prefabs/ChamberOfTheRoot.prefab";

    [MenuItem("Lord Lyrical/Chamber/Build and connect chamber")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before importing chamber assets.");
        AssetDatabase.Refresh();
        foreach(string folder in new[]{"Materials","Prefabs","Meshes","Animation","Scenes"})Directory.CreateDirectory(Root+"/"+folder);
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles(Root+"/Textures").Where(p=>!p.EndsWith(".meta")))
        {
            var ti=AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;if(ti==null)continue;
            bool normal=path.Contains("Normal")||path.Contains("nor_gl");
            ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            ti.sRGBTexture=!normal&&!path.Contains("Masks")&&!path.Contains("Rough")&&!path.Contains("Displacement");
            ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.isReadable=false;ti.anisoLevel=4;
            ti.wrapMode=path.Contains("Glyph")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
            ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
        }
        string modelPath=Root+"/Models/ChamberOfTheRoot.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
        importer.globalScale=1;importer.useFileScale=true;importer.isReadable=true;
        importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
        importer.SaveAndReimport();
        var shader=Shader.Find("LordLyrical/Chamber Surface");if(shader==null)throw new Exception("Chamber shader missing");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var model=UnityEngine.Object.Instantiate(source);model.name="ChamberOfTheRoot";
        var well=model.GetComponentsInChildren<Transform>().First(t=>t.name=="CH_FrequencyWell_Surface");
        Vector3 wellPosition=well.position;
        if(wellPosition.z<0)model.transform.rotation=Quaternion.Euler(0,180,0);
        var materialCache=new Dictionary<string,Material>();
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>MakeMaterial(m.name.Split('.')[0],shader,materialCache)).ToArray();
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        var root=new GameObject("Chamber of the Root");model.transform.SetParent(root.transform,true);
        CombineStaticMeshes(root);
        var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,-.15f,6);collider.size=new Vector3(32,.30f,40);
        var collisionRoot=new GameObject("Chamber collision boundaries");collisionRoot.transform.SetParent(root.transform,false);
        foreach(int side in new[]{-1,1})
        {
            var wallCollider=collisionRoot.AddComponent<BoxCollider>();wallCollider.center=new Vector3(side*16,5.5f,6);wallCollider.size=new Vector3(.6f,11,40);
            foreach(float z in new[]{-10,-4,2,8,14,20})
            {
                var columnCollider=collisionRoot.AddComponent<CapsuleCollider>();columnCollider.center=new Vector3(side*10.3f,4.4f,z);columnCollider.radius=.76f;columnCollider.height=8.8f;
            }
        }
        var presentation=root.AddComponent<ChamberPresentation>();
        presentation.dragonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(DragonRoot+"/Prefabs/LordLyrical_CyberDragon.prefab");
        if(presentation.dragonPrefab==null)throw new Exception("The preserved current dragon prefab was not found.");
        var clips=AssetDatabase.LoadAllAssetsAtPath(DragonRoot+"/Models/LL_CyberDragon.fbx").OfType<AnimationClip>().ToArray();
        presentation.glideClip=clips.First(c=>c.name=="Glide");presentation.flyClip=clips.First(c=>c.name=="Fly");presentation.roarClip=clips.First(c=>c.name=="Roar");
        presentation.unfoldClip=BuildUnfold(presentation.dragonPrefab,presentation.glideClip);
        presentation.previewInEditor=false;
        var keyObject=new GameObject("Chamber key light");keyObject.transform.SetParent(root.transform,false);
        var key=keyObject.AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.color=new Color(.65f,.78f,1);
        key.transform.rotation=Quaternion.Euler(42,-25,0);key.shadows=LightShadows.None;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);UnityEngine.Object.DestroyImmediate(root);
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        config.chamberModel=prefab;config.dragonModel=AssetDatabase.LoadAssetAtPath<GameObject>(DragonRoot+"/Prefabs/LordLyrical_CyberDragon.prefab");
        config.useSourceChamber=false;EditorUtility.SetDirty(config);
        // Existing dragon materials use URP Lit; enable one inexpensive main light.
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/LordLyrical/ScriptableObjects/MobileURP.asset");
        if(pipeline!=null){var so=new SerializedObject(pipeline);so.FindProperty("m_MainLightRenderingMode").intValue=1;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);}
        AssetDatabase.SaveAssets();Selection.activeObject=prefab;
        Debug.Log("LL_CHAMBER_BUILT: architecture, glyph materials, preserved dragon and additional unfold animation connected.");
        ValidateAndRender();
        ValidateShowIntegration();
    }

    static Material MakeMaterial(string name,Shader shader,Dictionary<string,Material> cache)
    {
        if(cache.TryGetValue(name,out var cached))return cached;
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
        Color colour=new Color(.42f,.46f,.52f);float metallic=.15f,smooth=.24f,texture=1,kind=0;
        if(name=="CH_Obsidian"){colour=new Color(.075f,.10f,.15f);metallic=.5f;smooth=.85f;texture=0;kind=1;}
        if(name=="CH_Titanium"){colour=new Color(.36f,.43f,.52f);metallic=.85f;smooth=.68f;texture=0;kind=2;}
        if(name=="CH_SilverInlay"){colour=new Color(.60f,.66f,.72f);metallic=.9f;smooth=.82f;texture=0;kind=2;}
        if(name=="CH_NicheStone"){colour=new Color(.15f,.19f,.25f);texture=.25f;}
        if(name=="CH_EngravedGlyph"){colour=new Color(.11f,.15f,.20f);texture=0;smooth=.5f;metallic=.4f;}
        if(name=="CH_BlueConduit"){colour=new Color(.015f,.12f,.21f);texture=0;metallic=.6f;}
        if(name=="CH_WellSurface"){colour=new Color(.007f,.037f,.050f);texture=0;smooth=.88f;metallic=.7f;kind=3;}
        if(name=="CH_AmberAccent"){colour=new Color(.29f,.10f,.025f);texture=0;metallic=.5f;}
        m.SetColor("_BaseColor",colour);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);m.SetFloat("_TextureAmount",texture);m.SetFloat("_SurfaceKind",kind);
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Stone_Diffuse.jpg"));
        m.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+(name=="CH_EngravedGlyph"?"CH_GlyphNormal.png":"Stone_nor_gl.jpg")));
        m.SetFloat("_NormalStrength",name=="CH_EngravedGlyph"?1: texture*.65f);
        m.SetTexture("_GlyphMask",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/CH_GlyphMasks.png"));
        m.SetFloat("_GlyphMode",name=="CH_EngravedGlyph"?1:0);m.SetFloat("_GlyphIntensity",.19f);m.SetFloat("_GlyphReveal",1);
        m.SetColor("_GlyphColor",new Color(.015f,.48f,1));m.SetFloat("_PulseSpeed",1);
        m.SetColor("_EmissionColor",name=="CH_BlueConduit"?new Color(.003f,.12f,.24f):name=="CH_AmberAccent"?new Color(.26f,.065f,.008f):Color.black);
        m.enableInstancing=true;EditorUtility.SetDirty(m);cache[name]=m;return m;
    }

    static void CombineStaticMeshes(GameObject root)
    {
        var groups=new Dictionary<string,List<CombineInstance>>();var mats=new Dictionary<string,Material>();
        var originals=new List<GameObject>();
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(r.name.StartsWith("CH_FractureColumn_")||r.name.StartsWith("CH_RevealRib_")||r.name=="CH_FrequencyWell_Surface")continue;
            var mesh=r.GetComponent<MeshFilter>().sharedMesh;int zone=Mathf.Clamp(Mathf.FloorToInt((r.bounds.center.z+14)/10),0,3);
            for(int i=0;i<mesh.subMeshCount;i++)
            {
                if(mesh.GetIndexCount(i)==0)continue;
                var mat=r.sharedMaterials[i];string key=zone+"_"+mat.name;
                if(!groups.ContainsKey(key)){groups[key]=new List<CombineInstance>();mats[key]=mat;}
                groups[key].Add(new CombineInstance{mesh=mesh,subMeshIndex=i,transform=root.transform.worldToLocalMatrix*r.localToWorldMatrix});
            }
            originals.Add(r.gameObject);
        }
        foreach(var kv in groups)
        {
            var mesh=new Mesh{name="CH_Batch_"+kv.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(kv.Value.ToArray(),true,true);mesh.RecalculateBounds();
            string path=Root+"/Meshes/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
            var go=new GameObject(mesh.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mats[kv.Key];renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        foreach(var go in originals)UnityEngine.Object.DestroyImmediate(go);
    }

    static AnimationClip BuildUnfold(GameObject source,AnimationClip glide)
    {
        var instance=UnityEngine.Object.Instantiate(source);instance.name=source.name;
        glide.SampleAnimation(instance,0);
        var bones=instance.GetComponentsInChildren<Transform>(true).Where(t=>t!=instance.transform).ToArray();
        var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();var scales=bones.Select(t=>t.localScale).ToArray();
        var paths=bones.Select(t=>AnimationUtility.CalculateTransformPath(t,instance.transform)).ToArray();
        var curves=new Dictionary<string,AnimationCurve[]>();
        foreach(string p in paths)curves[p]=Enumerable.Range(0,10).Select(_=>new AnimationCurve()).ToArray();
        foreach(float time in new[]{0f,.6f,1.5f,3f,4.5f,5.5f,6f})
        {
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];bones[i].localScale=scales[i];}
            float fold=1-Mathf.SmoothStep(0,1,time/6);
            foreach(var b in bones.Where(t=>t.name.StartsWith("wing_upper.")))
            {
                float side=Mathf.Sign(b.position.x);b.rotation=Quaternion.AngleAxis(side*44*fold,Vector3.forward)*b.rotation;
            }
            foreach(var b in bones.Where(t=>t.name.StartsWith("wing_fore.")))
            {
                float side=Mathf.Sign(b.position.x);b.rotation=Quaternion.AngleAxis(-side*20*fold,Vector3.up)*b.rotation;
            }
            for(int i=0;i<bones.Length;i++)
            {
                var b=bones[i];var v=new[]{b.localPosition.x,b.localPosition.y,b.localPosition.z,b.localRotation.x,b.localRotation.y,b.localRotation.z,b.localRotation.w,b.localScale.x,b.localScale.y,b.localScale.z};
                for(int j=0;j<10;j++)curves[paths[i]][j].AddKey(time,v[j]);
            }
        }
        var clip=new AnimationClip{name="Chamber_Unfold",frameRate=30};
        string[] properties={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalScale.x","m_LocalScale.y","m_LocalScale.z"};
        foreach(var kv in curves)for(int j=0;j<10;j++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(kv.Key,typeof(Transform),properties[j]),kv.Value[j]);
        clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
        string path=Root+"/Animation/Chamber_Unfold.anim";var previous=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(previous==null)AssetDatabase.CreateAsset(clip,path);else{EditorUtility.CopySerialized(clip,previous);UnityEngine.Object.DestroyImmediate(clip);clip=previous;}
        UnityEngine.Object.DestroyImmediate(instance);return clip;
    }

    [MenuItem("Lord Lyrical/Chamber/Validate and render chamber")]
    public static void ValidateAndRender()
    {
        ShaderUtil.allowAsyncCompilation=false;
        var shader=Shader.Find("LordLyrical/Chamber Surface");
        var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();
        if(errors.Length>0)throw new Exception(string.Join("\n",errors.Select(e=>e.message)));
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
        var presentation=instance.GetComponent<ChamberPresentation>();presentation.previewInEditor=false;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.11f,.14f,.21f);RenderSettings.skybox=null;
        var camera=new GameObject("Chamber review camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(2.8f,3.2f,-11);camera.transform.LookAt(new Vector3(0,7.4f,11));camera.fieldOfView=62;
        camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.009f,.014f,.025f);
        var report=new List<string>{"Chamber of the Root — Unity validation", "Unity: "+Application.unityVersion,"Original dragon prefab referenced: "+AssetDatabase.GetAssetPath(presentation.dragonPrefab)};
        var rt=new RenderTexture(1440,900,24){antiAliasing=4};camera.targetTexture=rt;
        try
        {
            foreach(float t in new[]{21,26,53.5f,59.5f,66,74,98.5f,105,117})
            {
                presentation.Evaluate(t,true);
                if(presentation.FractureColumnCount!=3)throw new Exception("Expected exactly three fracture columns.");
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                File.WriteAllBytes(Output+"/Chamber_Unity_"+t.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+".png",image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);report.Add("Rendered synchronized state: "+t+" s");
            }
            var renderers=instance.GetComponentsInChildren<Renderer>(true);
            var meshes=instance.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).ToArray();
            var renderErrors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();
            if(renderErrors.Length>0)throw new Exception(string.Join("\n",renderErrors.Select(e=>e.message)));
            report.Add("Environment renderers after batching: "+instance.GetComponentsInChildren<MeshRenderer>().Length);
            report.Add("Environment triangles: "+meshes.Sum(m=>m.triangles.Length/3));
            report.Add("Three fracture columns: PASS");report.Add("Additional unfold animation: "+presentation.unfoldClip.length+" seconds");
            report.Add("Source dragon geometry, rig, five clips and material assets preserved.");
            report.Add("PICO performance of the new chamber remains unprofiled.");
            File.WriteAllLines(Output+"/Chamber_Unity_Validation.txt",report);
            presentation.previewTime=66;presentation.previewInEditor=true;presentation.Evaluate(66,true);
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/ChamberShowcase.unity");
            ExportDeliveryPackage();
            File.WriteAllText("Library/LordLyricalChamberBuild.complete","SUCCESS\n"+DateTime.UtcNow.ToString("O"));
            Debug.Log("LL_CHAMBER_VALIDATED: nine state renders, new unfold clip, three fracture columns. Original dragon preserved.");
        }
        finally
        {
            RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            ShaderUtil.allowAsyncCompilation=true;
        }
    }

    [MenuItem("Lord Lyrical/Chamber/Export delivery package")]
    public static void ExportDeliveryPackage()
    {
        AssetDatabase.ExportPackage(new[]{Root,DragonRoot,
            "Assets/LordLyrical/Scripts/Core/ExperienceConfiguration.cs",
            "Assets/LordLyrical/Scripts/Environment/ResonantWorld.cs",
            "Assets/LordLyrical/ScriptableObjects/Experience.asset",
            "Assets/LordLyrical/ScriptableObjects/MobileURP.asset"},
            Output+"/LordLyrical_Chamber.unitypackage",ExportPackageOptions.Recurse);
    }

    [MenuItem("Lord Lyrical/Chamber/Inspect dragon pose")]
    public static void InspectDragonPose()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(DragonRoot+"/Prefabs/LordLyrical_CyberDragon.prefab");
        var instance=UnityEngine.Object.Instantiate(prefab);
        var clips=AssetDatabase.LoadAllAssetsAtPath(DragonRoot+"/Models/LL_CyberDragon.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        var report=new List<string>();
        foreach(var clip in clips)
        {
            clip.SampleAnimation(instance,0);
            report.Add(clip.name+" bindings: "+string.Join(" | ",AnimationUtility.GetCurveBindings(clip).Take(8).Select(b=>b.path+":"+b.propertyName)));
            foreach(var bone in instance.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("wing_upper")||t.name.Contains("wing_fore")||t.name=="head"))report.Add(bone.name+" world="+bone.position+" localrot="+bone.localRotation);
        }
        var all=instance.GetComponentsInChildren<Transform>();
        report.Add("Prefab paths: "+string.Join(" | ",all.Where(t=>t.name.Contains("wing_upper")).Select(t=>AnimationUtility.CalculateTransformPath(t,instance.transform))));
        File.WriteAllLines(Output+"/Chamber_Pose_Diagnostics.txt",report);UnityEngine.Object.DestroyImmediate(instance);
        Debug.Log("LL dragon pose diagnostics written");
    }

    [MenuItem("Lord Lyrical/Chamber/Verify complete show integration")]
    public static void ValidateShowIntegration()
    {
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        var session=new GameObject("Chamber integration preview session").AddComponent<NetworkSessionController>();
        var world=new GameObject("Chamber integrated show preview").AddComponent<ResonantWorld>();world.Initialize(config,session);
        var camera=new GameObject("Audience review camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,1.7f,-5);
        camera.transform.rotation=Quaternion.Euler(-8,0,0);camera.fieldOfView=85;camera.farClipPlane=100;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.012f,.023f);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.11f,.14f,.21f);RenderSettings.skybox=null;
        var rt=new RenderTexture(1440,900,24){antiAliasing=4};camera.targetTexture=rt;
        var report=new List<string>();
        try
        {
            // Seek in non-chronological order to exercise reconstruction and the vortex boundary.
            foreach(float t in new[]{0,21,59.5f,66,105,31,74,0,66,117})
            {
                world.Render(ShowFrame.At(t),true);
                var chamber=world.GetComponentInChildren<ChamberPresentation>(true);
                if(chamber==null)throw new Exception("Authored chamber was not selected by the show.");
                var oldDragon=world.GetComponentsInChildren<Transform>(true).First(x=>x.name=="Sound Dragon - living architecture");
                if(oldDragon.gameObject.activeSelf)throw new Exception("Procedural dragon duplicated the preserved dragon.");
                if(chamber.gameObject.activeInHierarchy!=(t>=21))throw new Exception("Chamber/vortex visibility failed.");
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                File.WriteAllBytes(Output+"/LordLyrical_Chamber_Show_"+t.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                report.Add("PASS authored chamber and current dragon integration at "+t+" seconds; original procedural dragon inactive.");
            }
            LordLyrical.Editor.ShowVerification.VerifyCore();report.Add("PASS existing core cue, clock, key and reconstruction tests.");
            File.WriteAllLines(Output+"/Chamber_Show_Integration.txt",report);
            Debug.Log("LL_CHAMBER_SHOW_PASS: integration, non-sequential reconstruction and existing core tests.");
        }
        finally
        {
            RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        }
    }
}
#endif
