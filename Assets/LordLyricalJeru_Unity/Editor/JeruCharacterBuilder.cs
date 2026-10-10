using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using LordLyrical.Characters;

public static class JeruCharacterBuilder
{
    const string Root="Assets/LordLyricalJeru_Unity";
    const string Fbx=Root+"/Models/LordLyrical_Jeru.fbx";
    const string Outputs=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\Jeru_Likeness";
    [MenuItem("Lord Lyrical/Jeru/Render Motion Preview")]
    public static void RenderMotionPreview()
    {
        var source=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            foreach(var root in source.GetRootGameObjects()){var clone=UnityEngine.Object.Instantiate(root);SceneManager.MoveGameObjectToScene(clone,scene);foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;}
            var p=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<JeruPerformance>()).First();
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();camera.cullingMask=1<<30;
            foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()))light.cullingMask=1<<30;
            string folder=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\work\jeru-preview-frames";Directory.CreateDirectory(folder);
            for(int f=0;f<96;f++)
            {
                if(f<36)p.SampleClip(1,f/12f);else if(f<72)p.SampleClip(2,(f-36)/9f);else p.SampleClip(5,(f-72)/12f);
                if(f<72){camera.transform.position=new Vector3(1.6f,1.7f,-3.8f);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=32;}
                else{camera.transform.position=new Vector3(.38f,1.64f,-1.12f);camera.transform.LookAt(new Vector3(0,1.62f,0));camera.fieldOfView=27;}
                p.SetSpectrum(Color.HSVToRGB(f/96f,.75f,1),1.3f);Render(camera,Path.Combine(folder,"Jeru_"+f.ToString("D4")+".png"));
            }
            Debug.Log("JERU_MOTION_PREVIEW_FRAMES_COMPLETE");
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(source);}
    }
    [MenuItem("Lord Lyrical/Jeru/Finalize Prefab Lighting")]
    public static void FinalizePrefabLighting()
    {
        string path=Root+"/Prefabs/LordLyrical_Jeru.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var lightTransform=root.transform.Find("Founder portrait light");
            var go=lightTransform?lightTransform.gameObject:new GameObject("Founder portrait light");go.transform.SetParent(root.transform,false);go.transform.localRotation=Quaternion.Euler(35,-30,0);
            var light=go.GetComponent<Light>();if(!light)light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.85f;light.color=new Color(1,.93f,.86f);light.shadows=LightShadows.None;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("JERU_PREFAB_LIGHTING_READY");
    }
    [MenuItem("Lord Lyrical/Jeru/Correct Facial Import")]
    public static void CorrectFacialImport()
    {
        var importer=(ModelImporter)AssetImporter.GetAtPath(Fbx);
        importer.importNormals=ModelImporterNormals.Import;
        importer.importBlendShapeNormals=ModelImporterNormals.Calculate;
        var description=importer.humanDescription;
        description.human=description.human.Where(h=>h.boneName!="jaw" && h.boneName!="eye_l" && h.boneName!="eye_r").ToArray();
        importer.humanDescription=description;importer.SaveAndReimport();
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(material.name.StartsWith("Jeru_")){material.SetFloat("_ReceiveShadows",0);material.EnableKeyword("_RECEIVE_SHADOWS_OFF");EditorUtility.SetDirty(material);}
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Jeru face normals calculated; jaw and gaze retain authored bone animation.");
    }
    [MenuItem("Lord Lyrical/Jeru/Validate Imported Animation")]
    public static void ValidateImportedAnimation()
    {
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/JeruCharacterReview.unity",OpenSceneMode.Additive);
        try
        {
            var p=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<JeruPerformance>()).First();
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
            var face=p.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.Contains("Jeru_Face"));
            int blink=face.sharedMesh.GetBlendShapeIndex("eyeBlinkLeft"),jaw=face.sharedMesh.GetBlendShapeIndex("jawOpen");
            camera.transform.position=new Vector3(.48f,1.64f,-1.12f);camera.transform.LookAt(new Vector3(0,1.62f,0));camera.fieldOfView=27;
            p.SampleClip(5,.5f);float blinkValue=face.GetBlendShapeWeight(blink);if(blinkValue<90)throw new Exception("Blink did not animate: "+blinkValue);
            Render(camera,Path.Combine(Outputs,"Jeru_Unity_Blink_Test.png"));
            p.SampleClip(5,1.5f);float jawValue=face.GetBlendShapeWeight(jaw);if(jawValue<90)throw new Exception("Jaw did not animate: "+jawValue);
            var first=new Mesh();face.BakeMesh(first);Render(camera,Path.Combine(Outputs,"Jeru_Unity_Jaw_Test.png"));
            p.SampleClip(5,8.5f);p.SampleClip(5,1.5f);var again=new Mesh();face.BakeMesh(again);float error=0;var av=first.vertices;var bv=again.vertices;for(int i=0;i<av.Length;i++)error=Mathf.Max(error,Vector3.Distance(av[i],bv[i]));
            p.SampleClip(5,0);var neutral=new Mesh();face.BakeMesh(neutral);float movement=0;var nv=neutral.vertices;for(int i=0;i<av.Length;i++)movement=Mathf.Max(movement,Vector3.Distance(av[i],nv[i]));
            if(movement<.01f || error>.0001f)throw new Exception("Facial deformation/seek test failed: "+movement+", "+error);
            Render(camera,Path.Combine(Outputs,"Jeru_Unity_Portrait.png"));
            p.SampleClip(2,2);float handY=p.animator.GetBoneTransform(HumanBodyBones.LeftHand).position.y;if(handY<1.4f)throw new Exception("Hand did not raise: "+handY);
            camera.transform.position=new Vector3(1.6f,1.70f,-3.8f);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=32;
            Render(camera,Path.Combine(Outputs,"Jeru_Unity_Raised_Hands.png"));
            p.SampleClip(1,1);Render(camera,Path.Combine(Outputs,"Jeru_Unity_FullBody.png"));
            File.WriteAllText(Path.Combine(Outputs,"Unity_Deformation_Validation.txt"),"PASS blink weight: "+blinkValue+"\nPASS jaw weight: "+jawValue+"\nPASS facial vertex displacement: "+movement+" metres\nPASS repeated seek error: "+error+" metres\nPASS raised left hand height: "+handY+" metres\n");
            UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(again);UnityEngine.Object.DestroyImmediate(neutral);
            Debug.Log("JERU_IMPORTED_ANIMATION_PASS");
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
    }
    [MenuItem("Lord Lyrical/Jeru/Connect to Lord Lyrical Show")]
    public static void ConnectShow()
    {
        var config=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
        if(!config)throw new Exception("Lord Lyrical configuration is not present.");
        var so=new SerializedObject(config);var p=so.FindProperty("lordModel");
        p.objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/LordLyrical_Jeru.prefab");so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
        Debug.Log("JERU_CONNECTED_TO_SHOW");
    }
    [MenuItem("Lord Lyrical/Jeru/Export Character Package")]
    public static void ExportPackage()
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);var animator=model.GetComponent<Animator>();var report=new StringBuilder("JERU FINAL UNITY IMPORT VALIDATION\n");
        report.AppendLine("Avatar valid: "+(animator&&animator.avatar&&animator.avatar.isValid));report.AppendLine("Humanoid: "+(animator&&animator.avatar&&animator.avatar.isHuman));
        foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())report.AppendLine(renderer.name+": "+renderer.sharedMesh.vertexCount+" vertices; "+renderer.sharedMesh.blendShapeCount+" shapes; "+renderer.bones.Length+" bones");
        foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"))){var bindings=AnimationUtility.GetCurveBindings(clip);report.AppendLine(clip.name+": "+clip.length+"s; "+bindings.Length+" bindings; facial bindings "+bindings.Count(b=>b.propertyName.StartsWith("blendShape.")));}
        report.AppendLine("Jaw and eyes preserve authored transform curves outside Humanoid facial retargeting. See Unity_Deformation_Validation.txt for sampled deformation assertions.");File.WriteAllText(Path.Combine(Outputs,"Unity_Import_Validation.txt"),report.ToString());
        AssetDatabase.ExportPackage(new[]{Root},Path.Combine(Path.GetDirectoryName(Outputs),"LordLyrical_Jeru.unitypackage"),ExportPackageOptions.Recurse);
        Debug.Log("JERU_PACKAGE_EXPORTED");
    }
    [MenuItem("Lord Lyrical/Jeru/Test Playback")]
    public static void TestPlayback()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/LordLyrical_Jeru.prefab"));
        var p=go.GetComponent<JeruPerformance>();var face=go.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.Contains("Jeru_Face"));
        var text=new StringBuilder();int ji=face.sharedMesh.GetBlendShapeIndex("jawOpen");int bi=face.sharedMesh.GetBlendShapeIndex("eyeBlinkLeft");
        foreach(float t in new[]{0f,.5f,1.5f,8.5f,1.5f})
        {
            p.SampleClip(5,t);text.AppendLine("t="+t+" animator initialized="+p.animator.isInitialized+" jaw="+face.GetBlendShapeWeight(ji)+" blink="+face.GetBlendShapeWeight(bi));
        }
        p.SampleClip(2,2);text.AppendLine("Raised left hand world="+p.animator.GetBoneTransform(HumanBodyBones.LeftHand).position);
        File.WriteAllText(Path.Combine(Outputs,"Unity_Playback_Test.txt"),text.ToString());UnityEngine.Object.DestroyImmediate(go);Debug.Log(text.ToString());
    }
    [MenuItem("Lord Lyrical/Jeru/Inspect Animation Curves")]
    public static void InspectCurves()
    {
        var text=new StringBuilder();
        foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
        {
            var bindings=AnimationUtility.GetCurveBindings(clip);int moving=0;
            foreach(var binding in bindings)
            {
                var curve=AnimationUtility.GetEditorCurve(clip,binding);if(curve==null || curve.length==0)continue;
                float min=curve.keys.Min(k=>k.value),max=curve.keys.Max(k=>k.value);
                if(max-min>.00001f)moving++;
                if(binding.propertyName.Contains("jawOpen") || binding.propertyName.Contains("eyeBlinkLeft"))text.AppendLine(clip.name+" "+binding.propertyName+" "+min+" to "+max+" at 1.5="+curve.Evaluate(1.5f));
            }
            text.AppendLine(clip.name+" varying curves "+moving);
        }
        File.WriteAllText(Path.Combine(Outputs,"Unity_Animation_Curves.txt"),text.ToString());Debug.Log(text.ToString());
    }
    [Serializable] class Manifest{public Mat[] materials;}
    [Serializable] class Mat{public string name,texture;public float[] color,emission;public float roughness,metallic,emissionStrength;}
    static Color C(float[] v)=>new Color(v[0],v[1],v[2],v.Length>3?v[3]:1);
    static void Folder(string path){Directory.CreateDirectory(path);}

    [MenuItem("Lord Lyrical/Jeru/Build Character and Review Scene")]
    public static void Build()
    {
        Folder(Root+"/Materials");Folder(Root+"/Prefabs");Folder(Root+"/Animations");Folder(Root+"/Scenes");
        AssetDatabase.Refresh();
        var importer=(ModelImporter)AssetImporter.GetAtPath(Fbx);
        importer.importBlendShapes=true;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
        importer.isReadable=true;importer.optimizeGameObjects=false;importer.maxBonesPerVertex=4;
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var boneMap=new Dictionary<HumanBodyBones,string>{
            {HumanBodyBones.Hips,"pelvis"},{HumanBodyBones.Spine,"spine_01"},{HumanBodyBones.Chest,"spine_02"},{HumanBodyBones.UpperChest,"spine_03"},
            {HumanBodyBones.Neck,"neck_01"},{HumanBodyBones.Head,"head"}};
        foreach(bool left in new[]{true,false})
        {
            string s=left?"_l":"_r",h=left?"Left":"Right";
            foreach(var pair in new[]{("Shoulder","clavicle"),("UpperArm","upperarm"),("LowerArm","lowerarm"),("Hand","hand"),("UpperLeg","thigh"),("LowerLeg","calf"),("Foot","foot"),("Toes","ball")})
                boneMap[(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),h+pair.Item1)]=pair.Item2+s;
            foreach(var finger in new[]{("Thumb","thumb"),("Index","index"),("Middle","middle"),("Ring","ring"),("Little","pinky")})
                for(int j=0;j<3;j++) boneMap[(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),h+finger.Item1+new[]{"Proximal","Intermediate","Distal"}[j])]=finger.Item2+"_0"+(j+1)+s;
        }
        var desc=new HumanDescription{
            human=boneMap.Select(p=>new HumanBone{humanName=HumanTrait.BoneName[(int)p.Key],boneName=p.Value,limit=new HumanLimit{useDefaultValues=true}}).ToArray(),
            skeleton=model.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
            upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false};
        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.humanDescription=desc;
        importer.importNormals=ModelImporterNormals.Import;importer.importBlendShapeNormals=ModelImporterNormals.Calculate;
        var defaults=importer.defaultClipAnimations;
        foreach(var clip in defaults)
        {
            int start=clip.name.LastIndexOf("Jeru_",StringComparison.Ordinal);if(start>=0)clip.name=clip.name.Substring(start);
            clip.loopTime=clip.name.Contains("Idle")||clip.name.Contains("Conduct");
            clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;clip.keepOriginalOrientation=true;
            clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;
        }
        importer.clipAnimations=defaults;importer.SaveAndReimport();
        model=AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/model_manifest.json"));
        var mats=new Dictionary<string,Material>();
        foreach(var entry in manifest.materials)
        {
            string path=Root+"/Materials/"+entry.name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.name=entry.name;mat.SetColor("_BaseColor",C(entry.color));mat.SetFloat("_Metallic",entry.metallic);mat.SetFloat("_Smoothness",1-entry.roughness);
            if(!string.IsNullOrEmpty(entry.texture))
            {
                string texturePath=Root+"/Textures/"+entry.texture;
                var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                if(ti!=null){ti.maxTextureSize=entry.name=="Jeru_Photo_Skin"?4096:2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();}
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }
            else mat.SetTexture("_BaseMap",null);
            if(entry.emissionStrength>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",C(entry.emission)*entry.emissionStrength);}
            else {mat.DisableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);}
            mat.SetFloat("_Cull",entry.name.Contains("Beard")?0:2);EditorUtility.SetDirty(mat);mats[entry.name]=mat;
            if(entry.name.StartsWith("Jeru_")){mat.SetFloat("_ReceiveShadows",0);mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");}
        }
        var stage=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject("Lord Lyrical - Jeru");SceneManager.MoveGameObjectToScene(root,stage);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,stage);instance.name="Jeru Rigged Model";instance.transform.SetParent(root.transform,false);
        instance.transform.localRotation=Quaternion.Euler(0,180,0);
        foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m&&mats.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();
        var animator=instance.GetComponent<Animator>();if(!animator)animator=instance.AddComponent<Animator>();
        animator.applyRootMotion=false;
        var clips=AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var performance=root.AddComponent<JeruPerformance>();performance.animator=animator;
        AnimationClip Find(string name)=>clips.FirstOrDefault(c=>c.name.EndsWith(name,StringComparison.Ordinal));
        performance.idle=Find("Jeru_Idle");performance.conduct=Find("Jeru_Conduct");performance.raiseTheWord=Find("Jeru_Raise_The_Word");performance.counterCall=Find("Jeru_Counter_Call");performance.turnToGroup=Find("Jeru_Turn_To_Group");performance.faceCalibration=Find("Jeru_Face_Calibration");
        if(!performance.idle || !performance.conduct)throw new Exception("Expected Jeru clips were not imported.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Animations/JeruPreview.controller");
        if(!controller) controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/Animations/JeruPreview.controller");
        foreach(var child in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(child.state);
        foreach(var clip in clips){var state=controller.layers[0].stateMachine.AddState(clip.name);state.motion=clip;if(clip==performance.idle)controller.layers[0].stateMachine.defaultState=state;}
        animator.runtimeAnimatorController=controller;
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/LordLyrical_Jeru.prefab");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Review floor";SceneManager.MoveGameObjectToScene(floor,stage);floor.transform.localScale=Vector3.one*.7f;
        var fm=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ReviewFloor.mat");if(!fm){fm=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(fm,Root+"/Materials/ReviewFloor.mat");}fm.SetColor("_BaseColor",new Color(.035f,.043f,.060f));fm.SetFloat("_Smoothness",.18f);floor.GetComponent<Renderer>().sharedMaterial=fm;
        var cameraObj=new GameObject("Jeru Review Camera");SceneManager.MoveGameObjectToScene(cameraObj,stage);var camera=cameraObj.AddComponent<Camera>();cameraObj.transform.position=new Vector3(2.4f,1.65f,-3.6f);cameraObj.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=32;camera.backgroundColor=new Color(.025f,.032f,.05f);camera.clearFlags=CameraClearFlags.SolidColor;
        foreach(var data in new[]{(new Vector3(-2,3,-2),1.6f,new Color(1,.90f,.80f)),(new Vector3(2,2,-1),.65f,new Color(.65f,.78f,1)),(new Vector3(0,3,2),1.2f,new Color(.50f,.72f,1))})
        {var go=new GameObject("Review soft light");SceneManager.MoveGameObjectToScene(go,stage);go.transform.position=data.Item1;go.transform.LookAt(new Vector3(0,1,0));var light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=data.Item2;light.color=data.Item3;light.shadows=LightShadows.Soft;light.cullingMask=1<<31;}
        foreach(var go in stage.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        camera.cullingMask=1<<31;
        EditorSceneManager.SaveScene(stage,Root+"/Scenes/JeruCharacterReview.unity");
        var sb=new StringBuilder();sb.AppendLine("JERU UNITY IMPORT VALIDATION");
        sb.AppendLine("Avatar valid: "+(animator.avatar&&animator.avatar.isValid));sb.AppendLine("Humanoid: "+(animator.avatar&&animator.avatar.isHuman));
        foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())sb.AppendLine(skin.name+": "+skin.sharedMesh.vertexCount+" vertices; "+skin.sharedMesh.blendShapeCount+" shapes; "+skin.bones.Length+" bones");
        foreach(var clip in clips){var bindings=AnimationUtility.GetCurveBindings(clip);sb.AppendLine(clip.name+": "+clip.length+"s; "+bindings.Length+" bindings; facial bindings "+bindings.Count(b=>b.propertyName.StartsWith("blendShape.")));}
        Directory.CreateDirectory(Outputs);File.WriteAllText(Path.Combine(Outputs,"Unity_Import_Validation.txt"),sb.ToString());
        AssetDatabase.SaveAssets();
        Render(camera,Path.Combine(Outputs,"Jeru_Unity_FullBody.png"));
        camera.transform.position=new Vector3(.48f,1.64f,-1.12f);camera.transform.LookAt(new Vector3(0,1.62f,0));camera.fieldOfView=27;
        Render(camera,Path.Combine(Outputs,"Jeru_Unity_Portrait.png"));
        performance.SampleClip(5,.5f);
        Render(camera,Path.Combine(Outputs,"Jeru_Unity_Blink_Test.png"));
        performance.SampleClip(5,1.5f);
        Render(camera,Path.Combine(Outputs,"Jeru_Unity_Jaw_Test.png"));
        var face=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.Contains("Jeru_Face"));
        var first=new Mesh();face.BakeMesh(first);
        performance.SampleClip(5,8.5f);performance.SampleClip(5,1.5f);
        var second=new Mesh();face.BakeMesh(second);
        float maxError=0;var a=first.vertices;var b=second.vertices;for(int i=0;i<a.Length;i++)maxError=Mathf.Max(maxError,Vector3.Distance(a[i],b[i]));
        File.AppendAllText(Path.Combine(Outputs,"Unity_Import_Validation.txt"),"Repeated facial seek maximum vertex error: "+maxError+" metres\n");
        UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second);
        performance.SampleClip(2,2f);camera.transform.position=new Vector3(1.6f,1.70f,-3.8f);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=32;
        Render(camera,Path.Combine(Outputs,"Jeru_Unity_Raised_Hands.png"));
        EditorSceneManager.CloseScene(stage,true);
        Debug.Log("JERU_BUILD_COMPLETE\n"+sb);
    }

    static void Render(Camera camera,string path)
    {
        // Bake a temporary render snapshot so editor-only captures refresh GPU skinning immediately.
        var skins=camera.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>()).Where(r=>r.enabled).ToArray();
        var snapshots=new List<GameObject>();var meshes=new List<Mesh>();
        foreach(var skin in skins)
        {
            var mesh=new Mesh();skin.BakeMesh(mesh);if(skin.sharedMesh.blendShapeCount>0)mesh.RecalculateNormals();meshes.Add(mesh);var go=new GameObject("Pose render snapshot");go.layer=skin.gameObject.layer;go.transform.SetParent(skin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=skin.sharedMaterials;var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block);mr.SetPropertyBlock(block);mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;snapshots.Add(go);skin.enabled=false;
        }
        var rt=new RenderTexture(1200,1200,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1200,1200,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
        foreach(var skin in skins)skin.enabled=true;foreach(var go in snapshots)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
    }
}
