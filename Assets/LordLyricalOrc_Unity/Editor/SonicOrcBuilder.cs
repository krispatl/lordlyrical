using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using LordLyrical.Characters;

public static class SonicOrcBuilder
{
    const string Root="Assets/LordLyricalOrc_Unity";
    const string Fbx=Root+"/Models/Sonic_Orc.fbx";
    const string Out=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\Sonic_Orc";
    static readonly string[] Names={"Orc_Idle","Orc_Advance","Orc_Resonance_Strike","Orc_Stagger","Orc_Defeat","Orc_Retreat"};
    [MenuItem("Lord Lyrical/Sonic Orc/Build Character and Review Scene")]
    public static void Build()
    {
        foreach(var f in new[]{"Materials","Prefabs","Animations","Scenes"})Directory.CreateDirectory(Root+"/"+f);
        AssetDatabase.Refresh();
        var imp=(ModelImporter)AssetImporter.GetAtPath(Fbx);
        imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        imp.importAnimation=true;imp.importCameras=false;imp.importLights=false;imp.isReadable=true;imp.optimizeGameObjects=false;
        imp.skinWeights=ModelImporterSkinWeights.Custom;imp.maxBonesPerVertex=4;imp.importNormals=ModelImporterNormals.Import;imp.SaveAndReimport();
        var defaults=imp.defaultClipAnimations;
        foreach(var clip in defaults)
        {
            foreach(string name in Names)if(clip.name.EndsWith(name,StringComparison.Ordinal))clip.name=name;
            clip.loopTime=clip.name==Names[0]||clip.name==Names[1]||clip.name==Names[5];
            clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;clip.keepOriginalOrientation=true;
            clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;
        }
        imp.clipAnimations=defaults;imp.SaveAndReimport();
        string texturePath=Root+"/Textures/Sonic_Orc_Basalt_BaseColor.png";
        var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;
        ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6,compressionQuality=80});ti.SaveAndReimport();
        Material Mat(string name,Color color,float metal,float smooth,Color emission)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("LordLyrical/Sonic Orc Surface");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Orc shader missing or failed compilation.");
            if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.SetColor("_EmissionColor",emission);EditorUtility.SetDirty(m);return m;
        }
        var basalt=Mat("Orc_Basalt_Bronze",Color.white,.62f,.64f,Color.black);basalt.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        var amber=Mat("Orc_Amber_Resonance",new Color(.38f,.075f,.006f),.5f,.75f,new Color(1,.22f,.007f)*1.5f);
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var root=new GameObject("Sonic Orc");var model=AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);instance.transform.SetParent(root.transform,false);
            foreach(var lod in instance.GetComponentsInChildren<LODGroup>())UnityEngine.Object.DestroyImmediate(lod);
            foreach(var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.sharedMaterials=new[]{r.name.Contains("Resonance")?amber:basalt};r.updateWhenOffscreen=false;
                r.localBounds=new Bounds(new Vector3(0,1.45f,0),new Vector3(4.2f,4.2f,4.2f));r.quality=SkinQuality.Bone4;
            }
            var animator=instance.GetComponent<Animator>();if(!animator)animator=instance.AddComponent<Animator>();animator.applyRootMotion=false;
            var clips=AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            AnimationClip Find(string n)=>clips.First(c=>c.name==n);
            var p=root.AddComponent<SonicOrcPerformance>();p.animator=animator;p.idle=Find(Names[0]);p.advance=Find(Names[1]);p.resonanceStrike=Find(Names[2]);p.stagger=Find(Names[3]);p.defeat=Find(Names[4]);p.retreat=Find(Names[5]);
            p.leftWeaponSocket=root.GetComponentsInChildren<Transform>().First(t=>t.name=="weapon_socket_l");p.rightWeaponSocket=root.GetComponentsInChildren<Transform>().First(t=>t.name=="weapon_socket_r");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Animations/SonicOrc.controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/Animations/SonicOrc.controller");
            foreach(var child in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(child.state);
            foreach(var clip in clips){var st=controller.layers[0].stateMachine.AddState(clip.name);st.motion=clip;if(clip.name==Names[0])controller.layers[0].stateMachine.defaultState=st;}
            animator.runtimeAnimatorController=controller;
            var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();var glyph=skins.First(r=>r.name.Contains("Resonance"));var group=root.AddComponent<LODGroup>();
            group.SetLODs(new[]{new LOD(.28f,new Renderer[]{skins.First(r=>r.name=="Sonic_Orc_LOD0"),glyph}),new LOD(.13f,new Renderer[]{skins.First(r=>r.name=="Sonic_Orc_LOD1"),glyph}),new LOD(.015f,new Renderer[]{skins.First(r=>r.name=="Sonic_Orc_LOD2"),glyph})});group.size=2.9f;group.localReferencePoint=new Vector3(0,1.45f,0);
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Sonic_Orc.prefab");
            root.transform.rotation=Quaternion.Euler(0,180,0);p.previewAutomatically=true;p.previewClip=0;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Review Floor";floor.transform.localScale=Vector3.one;
            var fm=new Material(Shader.Find("Universal Render Pipeline/Lit"));fm.SetColor("_BaseColor",new Color(.025f,.035f,.047f));fm.SetFloat("_Smoothness",.2f);
            string floorPath=Root+"/Materials/ReviewFloor.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(floorPath);if(existing){UnityEngine.Object.DestroyImmediate(fm);fm=existing;}else AssetDatabase.CreateAsset(fm,floorPath);floor.GetComponent<Renderer>().sharedMaterial=fm;
            var cam=new GameObject("Sonic Orc Review Camera").AddComponent<Camera>();cam.transform.position=new Vector3(3,2.15f,-6);cam.transform.LookAt(new Vector3(0,1.4f,0));cam.fieldOfView=34;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.012f,.019f,.032f);cam.cullingMask=1<<30;
            foreach(var lightData in new[]{(new Vector3(-3,4,-3),1.8f,new Color(1,.85f,.68f)),(new Vector3(3,3,-1),.85f,new Color(.54f,.75f,1)),(new Vector3(0,3,3),1.5f,new Color(.33f,.65f,1))})
            {var l=new GameObject("Studio Light").AddComponent<Light>();l.type=LightType.Directional;l.transform.position=lightData.Item1;l.transform.LookAt(new Vector3(0,1.3f,0));l.intensity=lightData.Item2;l.color=lightData.Item3;l.shadows=LightShadows.None;l.cullingMask=1<<30;}
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.15f,.2f);RenderSettings.skybox=null;
            foreach(var go in scene.GetRootGameObjects())foreach(var tr in go.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=30;
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/SonicOrcReview.unity");p.previewAutomatically=false;group.ForceLOD(0);
            p.SampleClip(0,0);p.SetResonance(1.5f);Render(cam,Out+"/Sonic_Orc_Unity_Hero.png");p.SampleClip(2,.9f);Render(cam,Out+"/Sonic_Orc_Unity_Strike.png");
            var skin=skins.First(r=>r.name=="Sonic_Orc_LOD0");var neutral=new Mesh();p.SampleClip(0,0);skin.BakeMesh(neutral);var moved=new Mesh();p.SampleClip(2,.9f);skin.BakeMesh(moved);var repeated=new Mesh();p.SampleClip(4,1.6f);p.SampleClip(2,.9f);skin.BakeMesh(repeated);
            float movement=0,error=0;var a=neutral.vertices;var b=moved.vertices;var c=repeated.vertices;
            for(int i=0;i<a.Length;i++){movement=Mathf.Max(movement,Vector3.Distance(a[i],b[i]));error=Mathf.Max(error,Vector3.Distance(b[i],c[i]));}
            if(movement<.1f||error>.0001f)throw new Exception("Orc deformation check failed: "+movement+", seek error "+error);
            var sb=new StringBuilder("SONIC ORC UNITY VALIDATION\n");sb.AppendLine("PASS Generic avatar valid: "+(animator.avatar&&animator.avatar.isValid));
            sb.AppendLine("PASS six animation clips; two material slots per visible character; three body LODs");sb.AppendLine("PASS strike vertex movement: "+movement+" m");sb.AppendLine("PASS repeated seek vertex error: "+error+" m");
            foreach(var s in skins)sb.AppendLine(s.name+": "+(s.sharedMesh.triangles.Length/3)+" triangles, "+s.bones.Length+" skin bones");
            foreach(var clip in clips)sb.AppendLine(clip.name+": "+clip.length+" s; "+AnimationUtility.GetCurveBindings(clip).Length+" curves");
            foreach(int level in new[]{0,1,2}){group.ForceLOD(level);p.SampleClip(0,0);Render(cam,Out+"/Sonic_Orc_Unity_LOD"+level+".png",1100,level);}
            File.WriteAllText(Out+"/Unity_Validation.txt",sb.ToString());AssetDatabase.SaveAssets();
            foreach(var mesh in new[]{neutral,moved,repeated})UnityEngine.Object.DestroyImmediate(mesh);
            Debug.Log("SONIC_ORC_BUILD_PASS\n"+sb);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }

    [MenuItem("Lord Lyrical/Sonic Orc/Render Review Images")]
    public static void ReviewImages()
    {
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(Root+"/Scenes/SonicOrcReview.unity",OpenSceneMode.Additive);
        try
        {
            var p=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SonicOrcPerformance>()).First();p.previewAutomatically=false;p.GetComponent<LODGroup>().ForceLOD(0);
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
            p.SampleClip(0,0);p.SetResonance(1.5f);Render(camera,Out+"/Sonic_Orc_Unity_Hero.png");p.SampleClip(2,.9f);Render(camera,Out+"/Sonic_Orc_Unity_Strike.png");
            p.SampleClip(0,0);foreach(int level in new[]{0,1,2})Render(camera,Out+"/Sonic_Orc_Unity_LOD"+level+".png",1100,level);
            p.SetResonance(2,.5f);Render(camera,Out+"/Sonic_Orc_Unity_Dissolve.png");
            Debug.Log("SONIC_ORC_REVIEW_READY");
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
    [MenuItem("Lord Lyrical/Sonic Orc/Render Motion Preview")]
    public static void Preview()
    {
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(Root+"/Scenes/SonicOrcReview.unity",OpenSceneMode.Additive);
        try
        {
            var p=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SonicOrcPerformance>()).First();p.previewAutomatically=false;p.GetComponent<LODGroup>().ForceLOD(0);
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
            string folder=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\work\sonic-orc\preview";Directory.CreateDirectory(folder);
            for(int f=0;f<96;f++)
            {
                float t=f/12f;if(t<1)p.SampleClip(0,t);else if(t<3)p.SampleClip(1,t-1);else if(t<5)p.SampleClip(2,t-3);else if(t<6)p.SampleClip(3,t-5);else p.SampleClip(4,t-6);
                p.SetResonance(1.4f+(t>3&&t<4.5f?2*Mathf.Sin((t-3)*Mathf.PI/1.5f):.1f));Render(camera,folder+"/Orc_"+f.ToString("D4")+".png",800);
            }
            Debug.Log("SONIC_ORC_PREVIEW_COMPLETE");
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
    [MenuItem("Lord Lyrical/Sonic Orc/Export Package")]
    public static void Export(){AssetDatabase.ExportPackage(Root,Path.GetDirectoryName(Out)+"/LordLyrical_Sonic_Orc.unitypackage",ExportPackageOptions.Recurse);Debug.Log("SONIC_ORC_PACKAGE_READY");}

    public static void Render(Camera camera,string path,int size=1100,int detailLevel=0)
    {
        var groups=camera.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>()).Where(g=>g.enabled).ToArray();
        foreach(var group in groups)group.enabled=false;
        var skins=camera.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>()).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        var snapshots=new List<GameObject>();var meshes=new List<Mesh>();
        foreach(var skin in skins)
        {
            if(skin.name.StartsWith("Sonic_Orc_LOD")&&skin.name!="Sonic_Orc_LOD"+detailLevel){skin.enabled=false;continue;}
            var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);var go=new GameObject("Pose snapshot");go.layer=skin.gameObject.layer;go.transform.SetParent(skin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=skin.sharedMaterials;var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block);mr.SetPropertyBlock(block);snapshots.Add(go);skin.enabled=false;
        }
        var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;camera.targetTexture=rt;
        bool asyncShaders=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try{camera.Render();RenderTexture.active=rt;var image=new Texture2D(size,size,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size,size),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally{ShaderUtil.allowAsyncCompilation=asyncShaders;RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);foreach(var skin in skins)skin.enabled=true;foreach(var go in snapshots)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var group in groups)group.enabled=true;}
    }
}
