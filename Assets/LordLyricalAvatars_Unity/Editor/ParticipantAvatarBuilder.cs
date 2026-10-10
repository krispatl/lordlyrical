using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical.Characters;

public static class ParticipantAvatarBuilder
{
    public const string Root="Assets/LordLyricalAvatars_Unity";
    public const string Output=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\Participant_Avatars";
    public static readonly string[] Ids={"D_Warrior","C_Foundation","E_Blade","G_Balanced","F_Healer","B_Shadow","Cs_Seeker","Fs_Architect","Bb_Discordant","A_Ancient"};
    [Serializable] public class Manifest {public string id,name,key,family,hex,design,ability;public int slot,bones;public float height;public Mat[] materials;}
    [Serializable] public class Mat {public string name,texture;public float[] color,emission;public float roughness,metallic,strength;}
    static Color C(float[] v)=>new Color(v[0],v[1],v[2],v.Length>3?v[3]:1);
    static readonly string[] Suffixes={"Idle","Walk","Ability","Stagger","Rise","Hands"};
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static HumanDescription Human(GameObject model)
    {
        var map=new Dictionary<HumanBodyBones,string>{{HumanBodyBones.Hips,"pelvis"},{HumanBodyBones.Spine,"spine_01"},{HumanBodyBones.Chest,"spine_02"},{HumanBodyBones.UpperChest,"spine_03"},{HumanBodyBones.Neck,"neck_01"},{HumanBodyBones.Head,"head"}};
        foreach(bool left in new[]{true,false}){
            string s=left?"_l":"_r",h=left?"Left":"Right";
            foreach(var pair in new[]{("Shoulder","clavicle"),("UpperArm","upperarm"),("LowerArm","lowerarm"),("Hand","hand"),("UpperLeg","thigh"),("LowerLeg","calf"),("Foot","foot"),("Toes","ball")})map[(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),h+pair.Item1)]=pair.Item2+s;
            foreach(var finger in new[]{("Thumb","thumb"),("Index","index"),("Middle","middle"),("Ring","ring"),("Little","pinky")})for(int j=0;j<3;j++)map[(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),h+finger.Item1+new[]{"Proximal","Intermediate","Distal"}[j])]=finger.Item2+"_0"+(j+1)+s;
        }
        return new HumanDescription{human=map.Select(p=>new HumanBone{humanName=HumanTrait.BoneName[(int)p.Key],boneName=p.Value,limit=new HumanLimit{useDefaultValues=true}}).ToArray(),skeleton=model.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false};
    }
    [MenuItem("Lord Lyrical/Participants/Build Ten Avatars")]
    public static void Build()
    {
        foreach(string dir in new[]{"Materials","Prefabs","Animations","Scenes"})Directory.CreateDirectory(Root+"/"+dir);
        AssetDatabase.Refresh();Directory.CreateDirectory(Output);
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Textures"})){
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});importer.SaveAndReimport();
        }
        var before=SceneManager.GetActiveScene();var stage=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(stage);
        var report=new StringBuilder("TEN ROOT KEY PARTICIPANTS - UNITY IMPORT\n");
        try{for(int slot=0;slot<Ids.Length;slot++){
            string id=Ids[slot],fbx=Root+"/Models/"+id+".fbx";var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/"+id+"_manifest.json"));
            var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);Require(importer!=null,"Model missing: "+id);
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.optimizeGameObjects=false;
            importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=4;importer.importNormals=ModelImporterNormals.Import;
            importer.animationType=ModelImporterAnimationType.Generic;importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var human=Human(model);importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.humanDescription=human;
            var entries=importer.defaultClipAnimations;
            foreach(var c in entries){int start=c.name.LastIndexOf(id+"_",StringComparison.Ordinal);if(start>=0)c.name=c.name.Substring(start);c.loopTime=c.name.EndsWith("Idle")||c.name.EndsWith("Walk")||c.name.EndsWith("Hands");c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;c.keepOriginalOrientation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.lockRootRotation=true;}
            importer.clipAnimations=entries;importer.SaveAndReimport();model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var mats=new Dictionary<string,Material>();
            foreach(var entry in manifest.materials){
                string path=Root+"/Materials/"+entry.name.Replace(" ","_")+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.name=entry.name;
                mat.SetColor("_BaseColor",C(entry.color));mat.SetFloat("_Metallic",entry.metallic);mat.SetFloat("_Smoothness",1-entry.roughness);mat.SetFloat("_Cull",entry.name.Contains("Vestment")?0:2);
                mat.SetTexture("_BaseMap",string.IsNullOrEmpty(entry.texture)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+entry.texture));
                if(entry.strength>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",C(entry.emission)*entry.strength);}else{mat.DisableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);}
                EditorUtility.SetDirty(mat);mats[entry.name]=mat;
            }
            foreach(var entry in mats)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),entry.Key),entry.Value);
            importer.SaveAndReimport();model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root=new GameObject(manifest.key+" - "+manifest.name);var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,stage);instance.name="Rigged model";instance.transform.SetParent(root.transform,false);
            foreach(var r in instance.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>m&&mats.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();
            var animator=instance.GetComponent<Animator>();Require(animator&&animator.avatar&&animator.avatar.isValid&&animator.avatar.isHuman,id+" Humanoid avatar invalid");animator.applyRootMotion=false;
            var clips=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();Require(clips.Length==6,id+" expected six clips, found "+clips.Length);
            AnimationClip Find(string suffix)=>clips.First(c=>c.name==id+"_"+suffix);
            var p=root.AddComponent<ParticipantAvatarPerformance>();p.slot=slot;p.roleName=manifest.name;p.rootKey=manifest.key;ColorUtility.TryParseHtmlString("#"+manifest.hex,out p.roleColor);p.animator=animator;
            p.idle=Find("Idle");p.walk=Find("Walk");p.ability=Find("Ability");p.stagger=Find("Stagger");p.rise=Find("Rise");p.hands=Find("Hands");
            var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer Mesh(string name)=>renderers.First(r=>r.name==name);
            p.bodyLevels=Enumerable.Range(0,3).Select(n=>Mesh("Avatar_Body_LOD"+n)).ToArray();p.energyMesh=Mesh("Avatar_Cymatic");p.leftHandMesh=Mesh("Avatar_LeftHand");p.rightHandMesh=Mesh("Avatar_RightHand");
            foreach(var r in renderers){r.updateWhenOffscreen=true;r.localBounds=new Bounds(new Vector3(0,manifest.height*.55f,0),Vector3.one*manifest.height*2);r.quality=SkinQuality.Bone4;}
            p.bodyLod=root.AddComponent<LODGroup>();p.bodyLod.SetLODs(new[]{new LOD(.40f,new Renderer[]{p.bodyLevels[0]}),new LOD(.16f,new Renderer[]{p.bodyLevels[1]}),new LOD(.005f,new Renderer[]{p.bodyLevels[2]})});p.bodyLod.RecalculateBounds();
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Animations/"+id+".controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/Animations/"+id+".controller");
            foreach(var child in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(child.state);
            foreach(string suffix in Suffixes){var state=controller.layers[0].stateMachine.AddState(suffix);state.motion=Find(suffix);if(suffix=="Idle")controller.layers[0].stateMachine.defaultState=state;}animator.runtimeAnimatorController=controller;
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+id+".prefab");
            report.AppendLine("PASS "+id+": valid Humanoid, "+renderers.Length+" skins, six clips, three LODs, separate hands");
            foreach(var r in p.bodyLevels)report.AppendLine("  "+r.name+": "+r.sharedMesh.triangles.Length/3+" triangles");
            root.transform.position=new Vector3((slot%5-2)*2.2f,0,(slot/5)*3.6f);root.transform.rotation=Quaternion.Euler(0,12,0);p.SampleClip(0,.7f);p.SetResonance(1);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            Debug.Log("AVATAR_IMPORTED "+id);
        }
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Gallery floor";floor.transform.position=new Vector3(0,-.03f,1.5f);floor.transform.localScale=new Vector3(1.5f,1,1);floor.layer=30;
        var fm=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/GalleryFloor.mat");if(!fm){fm=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(fm,Root+"/Materials/GalleryFloor.mat");}fm.SetColor("_BaseColor",new Color(.018f,.025f,.037f));fm.SetFloat("_Smoothness",.20f);floor.GetComponent<Renderer>().sharedMaterial=fm;
        var camera=new GameObject("Participant gallery camera").AddComponent<Camera>();camera.transform.position=new Vector3(5,6.6f,13);camera.transform.LookAt(new Vector3(0,.8f,1.8f));camera.orthographic=true;camera.orthographicSize=4.0f;camera.backgroundColor=new Color(.018f,.025f,.037f);camera.clearFlags=CameraClearFlags.SolidColor;camera.cullingMask=1<<30;
        foreach(var item in new[]{(new Vector3(25,155,0),2.0f,new Color(1,.94f,.86f)),(new Vector3(35,-35,0),.85f,new Color(.5f,.7f,1))}){var light=new GameObject("Gallery light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(item.Item1);light.intensity=item.Item2;light.color=item.Item3;light.cullingMask=1<<30;light.shadows=LightShadows.Soft;}
        EditorSceneManager.SaveScene(stage,Root+"/Scenes/ParticipantAvatarReview.unity");AssetDatabase.SaveAssets();
        File.WriteAllText(Output+"/Unity_Import_Validation.txt",report.ToString());
        Render(camera,Output+"/All_Ten_Unity_Gallery.png",2000,1100,0);
        Debug.Log("TEN_AVATARS_BUILD_COMPLETE\n"+report);
        }finally{EditorSceneManager.CloseScene(stage,true);SceneManager.SetActiveScene(before);}
    }
    [MenuItem("Lord Lyrical/Participants/Validate Poses and Render Portraits")]
    public static void Validate()
    {
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(Root+"/Scenes/ParticipantAvatarReview.unity",OpenSceneMode.Additive);
        try{
            var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParticipantAvatarPerformance>(true)).OrderBy(p=>p.slot).ToArray();var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
            var report=new StringBuilder("PARTICIPANT DEFORMATION, SEEK AND FIRST-PERSON VALIDATION\n");
            foreach(var p in actors)p.gameObject.SetActive(false);
            foreach(var p in actors){
                p.gameObject.SetActive(true);p.transform.position=Vector3.zero;p.transform.rotation=Quaternion.Euler(0,15,0);p.SetFirstPerson(false);p.SampleClip(0,.7f);p.SetResonance(.6f);
                camera.transform.position=new Vector3(2.5f,1.6f,6);camera.transform.LookAt(Vector3.up*1.05f);camera.orthographicSize=1.35f;
                var body=p.bodyLevels[0];Vector3[] Pose(int clip,float t){p.SampleClip(clip,t);var m=new Mesh();body.BakeMesh(m);var points=m.vertices;UnityEngine.Object.DestroyImmediate(m);return points;}
                var neutral=Pose(0,.7f);Render(camera,Output+"/"+Ids[p.slot]+"_Unity.png",800,1000,0);
                var ability=Pose(2,1.5f);float movement=neutral.Zip(ability,(a,b)=>Vector3.Distance(a,b)).Max();Require(movement>.02f,p.roleName+" ability did not deform body");
                Render(camera,Output+"/"+Ids[p.slot]+"_Ability.png",800,1000,0);Pose(4,3.9f);var again=Pose(2,1.5f);float seek=again.Zip(ability,(a,b)=>Vector3.Distance(a,b)).Max();Require(seek<.0001f,p.roleName+" seek mismatch");
                Pose(4,3.99f);var hand=p.animator.GetBoneTransform(HumanBodyBones.LeftHand);float raised=hand.position.y;var held=Pose(4,10);Require(Mathf.Abs(raised-hand.position.y)<.005f,p.roleName+" Rise looped instead of holding");
                Render(camera,Output+"/"+Ids[p.slot]+"_Rise.png",800,1000,0);
                p.SampleShow(40);Require(p.IsNarrativelyHidden==(p.slot==5),"Shadow visibility");p.SampleShow(51);Require(!p.IsNarrativelyHidden,"Shadow failed to return");
                p.SetFirstPerson(true);Require(p.bodyLevels.All(r=>!r.enabled)&&!p.energyMesh.enabled&&p.leftHandMesh.enabled&&p.rightHandMesh.enabled,"First-person visibility mismatch");
                var left=new GameObject("Validation left target").transform;var right=new GameObject("Validation right target").transform;left.position=new Vector3(-.3f,1.25f,.4f);right.position=new Vector3(.3f,1.3f,.4f);
                p.ApplyTrackedHands(left,right);float error=Mathf.Max(Vector3.Distance(p.animator.GetBoneTransform(HumanBodyBones.LeftHand).position,left.position),Vector3.Distance(p.animator.GetBoneTransform(HumanBodyBones.RightHand).position,right.position));Require(error<.0001f,"Tracked hand target error");
                UnityEngine.Object.DestroyImmediate(left.gameObject);UnityEngine.Object.DestroyImmediate(right.gameObject);
                p.SetFirstPerson(false);p.SampleClip(0,.7f);report.AppendLine($"PASS {Ids[p.slot]}: ability displacement {movement:F4}m; repeat seek {seek:F6}m; raised hand {raised:F3}m; tracked hand error {error:F6}m; first-person isolation");
                p.gameObject.SetActive(false);
            }
            File.WriteAllText(Output+"/Unity_Pose_Validation.txt",report.ToString());Debug.Log("TEN_AVATARS_POSE_PASS\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
    public static void Render(Camera camera,string path,int width=1600,int height=1000,int lod=0)
    {
        var groups=camera.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>()).ToArray();
        var grouped=new HashSet<Renderer>();var selected=new HashSet<Renderer>();
        foreach(var group in groups){var levels=group.GetLODs();foreach(var level in levels)foreach(var r in level.renderers)grouped.Add(r);if(levels.Length>0)foreach(var r in levels[Mathf.Clamp(lod,0,levels.Length-1)].renderers)selected.Add(r);}
        // Use the actual LOD membership; name filtering drew LOD0 together with a
        // requested distance level. BakeMesh(true) compensates for scaled actors.
        var skins=camera.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>()).Where(r=>r.enabled&&(!grouped.Contains(r)||selected.Contains(r))).ToArray();
        var groupStates=groups.Select(g=>g.enabled).ToArray();foreach(var g in groups)g.enabled=false;
        var groupedRenderers=grouped.Where(r=>r).ToArray();var groupedEnabled=groupedRenderers.Select(r=>r.enabled).ToArray();
        foreach(var r in groupedRenderers)r.enabled=selected.Contains(r);
        var all=camera.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>()).ToArray();var enabled=all.Select(r=>r.enabled).ToArray();
        var snapshots=new List<GameObject>();var meshes=new List<Mesh>();bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        RenderTexture old=RenderTexture.active;var oldTarget=camera.targetTexture;RenderTexture rt=null;Texture2D tex=null;
        try{foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh,true);meshes.Add(mesh);var go=new GameObject("Capture pose");go.layer=skin.gameObject.layer;go.transform.SetParent(skin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=skin.sharedMaterials;var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block);r.SetPropertyBlock(block);snapshots.Add(go);}
            foreach(var r in all)r.enabled=false;
            rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex=new Texture2D(width,height,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
        }finally{RenderTexture.active=old;camera.targetTexture=oldTarget;if(tex)UnityEngine.Object.DestroyImmediate(tex);if(rt)UnityEngine.Object.DestroyImmediate(rt);foreach(var go in snapshots)UnityEngine.Object.DestroyImmediate(go);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);for(int i=0;i<all.Length;i++)all[i].enabled=enabled[i];for(int i=0;i<groupedRenderers.Length;i++)groupedRenderers[i].enabled=groupedEnabled[i];for(int i=0;i<groups.Length;i++)groups[i].enabled=groupStates[i];ShaderUtil.allowAsyncCompilation=async;}
    }
    [MenuItem("Lord Lyrical/Participants/Polish Review Lighting")]
    public static void PolishReview()
    {
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/ParticipantAvatarReview.unity",OpenSceneMode.Additive);
        try{
            var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()).ToArray();lights[0].transform.rotation=Quaternion.Euler(25,155,0);lights[0].intensity=2.0f;lights[0].color=new Color(1,.94f,.86f);lights[1].transform.rotation=Quaternion.Euler(35,-35,0);lights[1].intensity=.85f;
            var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParticipantAvatarPerformance>()).ToArray();var report=new StringBuilder();
            foreach(var p in actors){report.AppendLine(p.roleName);foreach(var m in p.bodyLevels[0].sharedMaterials)report.AppendLine(m.name+" | "+m.shader.name+" | "+m.GetColor("_BaseColor"));}
            File.WriteAllText(Output+"/Material_Diagnostics.txt",report.ToString());
            EditorSceneManager.SaveScene(scene);var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();foreach(var p in actors){p.SampleClip(0,.7f);p.SetResonance(1);}Render(camera,Output+"/All_Ten_Unity_Gallery.png",2000,1100,0);
        }finally{EditorSceneManager.CloseScene(scene,true);}
    }
    [MenuItem("Lord Lyrical/Participants/Render Motion Preview")]
    public static void MotionPreview()
    {
        string folder=@"C:\Users\kris\Documents\Codex\2026-10-03\bui\work\avatars\motion";Directory.CreateDirectory(folder);
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/ParticipantAvatarReview.unity",OpenSceneMode.Additive);
        try{
            var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParticipantAvatarPerformance>()).ToArray();var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
            for(int f=0;f<120;f++){
                float t=f/15f;foreach(var p in actors){p.SampleClip(t<2?0:t<5?2:4,t<2?t:t<5?t-2:(t-5)*1.34f);p.SetResonance(t<5?1:2);}
                Render(camera,folder+"/Avatars_"+f.ToString("D4")+".png",1280,720,0);
            }
            Debug.Log("TEN_AVATARS_MOTION_FRAMES_COMPLETE");
        }finally{EditorSceneManager.CloseScene(scene,true);}
    }
    [MenuItem("Lord Lyrical/Participants/Finalize Frequency Gloves")]
    public static void Gloves()
    {
        void Set(ParticipantAvatarPerformance p){
            string path=Root+"/Materials/"+Ids[p.slot]+"_Hands.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",Color.Lerp(new Color(.03f,.03f,.035f),p.roleColor,.25f));mat.SetFloat("_Metallic",.22f);mat.SetFloat("_Smoothness",.38f);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",p.roleColor*.25f);EditorUtility.SetDirty(mat);p.leftHandMesh.sharedMaterial=mat;p.rightHandMesh.sharedMaterial=mat;
        }
        foreach(string id in Ids){string path=Root+"/Prefabs/"+id+".prefab";var go=PrefabUtility.LoadPrefabContents(path);try{Set(go.GetComponent<ParticipantAvatarPerformance>());PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}}
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/ParticipantAvatarReview.unity",OpenSceneMode.Additive);
        try{foreach(var p in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParticipantAvatarPerformance>()))Set(p);EditorSceneManager.SaveScene(scene);}finally{EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();Debug.Log("TEN_AVATARS_GLOVES_READY");
    }
    [MenuItem("Lord Lyrical/Participants/Validate Finger and Tracking Deformation")]
    public static void Tracking()
    {
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("FINGER DEFORMATION AND TRACKING VALIDATION\n");
        try{foreach(string id in Ids){
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+id+".prefab"));var p=go.GetComponent<ParticipantAvatarPerformance>();
            Vector3[] Points(){var mesh=new Mesh();p.leftHandMesh.BakeMesh(mesh);var points=mesh.vertices;UnityEngine.Object.DestroyImmediate(mesh);return points;}
            p.SampleClip(5,0);var open=Points();p.SampleClip(5,1);float curl=open.Zip(Points(),(a,b)=>Vector3.Distance(a,b)).Max();Require(curl>.003f,id+" fingers did not curl");
            p.SampleClip(0,.7f);var basePose=Points();var head=new GameObject("Synthetic HMD").transform;head.position=new Vector3(0,1.65f,0);var left=new GameObject("Synthetic left controller").transform;var right=new GameObject("Synthetic right controller").transform;left.position=new Vector3(-.30f,1.20f,.25f);right.position=new Vector3(.30f,1.20f,.25f);
            p.SetFirstPerson(true);p.ApplyTrackedHands(left,right);p.SampleClip(0,.7f);float restore=basePose.Zip(Points(),(a,b)=>Vector3.Distance(a,b)).Max();Require(restore<.001f,id+" tracking did not restore animation pose");
            p.SetFirstPerson(false);p.ApplyTrackedBody(head,left,right,0);float reach=Mathf.Max(Vector3.Distance(left.position,p.animator.GetBoneTransform(HumanBodyBones.LeftHand).position),Vector3.Distance(right.position,p.animator.GetBoneTransform(HumanBodyBones.RightHand).position));Require(reach<.04f,id+" typical remote controller reach error "+reach);
            Require(head.position==new Vector3(0,1.65f,0)&&left.position==new Vector3(-.30f,1.20f,.25f),"Physical tracking target moved");
            report.AppendLine($"PASS {id}: finger displacement {curl:F4}m; pose restored after tracking {restore:F6}m; remote wrist target error {reach:F4}m; XR targets unchanged");
            UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(head.gameObject);UnityEngine.Object.DestroyImmediate(left.gameObject);UnityEngine.Object.DestroyImmediate(right.gameObject);
        }File.WriteAllText(Output+"/Unity_Tracking_Validation.txt",report.ToString());Debug.Log("TEN_AVATARS_TRACKING_PASS\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
    [MenuItem("Lord Lyrical/Participants/Export Ten Avatar Package")]
    public static void Export(){AssetDatabase.SaveAssets();AssetDatabase.ExportPackage(new[]{Root,"Assets/LordLyricalFinishing_Unity/Textures"}.Concat(Ids.Select(id=>"Assets/LordLyricalFinishing_Unity/Models/"+id+"_Distance_Hands.fbx")).ToArray(),Path.Combine(Path.GetDirectoryName(Output),"LordLyrical_Ten_Avatars.unitypackage"),ExportPackageOptions.Recurse);Debug.Log("TEN_AVATARS_PACKAGE_EXPORTED");}
}
