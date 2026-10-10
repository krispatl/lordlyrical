#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;

public static class LordLyricalDragonSetup
{
    const string Folder = "Assets/LordLyricalDragon_Unity";
    const string MainPath = Folder + "/Models/LL_CyberDragon.fbx";
    static readonly string[] Motions = { "Idle", "Fly", "Glide", "Roar", "Bite" };

    [MenuItem("Tools/Lord Lyrical/Build Dragon Prefab")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory(Folder + "/Materials");
        Directory.CreateDirectory(Folder + "/Prefabs");
        Directory.CreateDirectory(Folder + "/Animation");
        AssetDatabase.Refresh();
        for (int level = 0; level < 3; level++)
        {
            string path = level == 0 ? MainPath : Folder + "/Models/LL_CyberDragon_LOD" + level + ".fbx";
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) throw new InvalidOperationException("Missing dragon model: " + path);
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.motionNodeName = "root";
            imp.importAnimation = level == 0;
            imp.globalScale = 1f;
            imp.useFileScale = true;
            imp.optimizeGameObjects = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.isReadable = false;
            imp.importBlendShapes = false;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.CalculateMikk;
            imp.animationCompression = ModelImporterAnimationCompression.Off;
            if (level == 0)
            {
                imp.SaveAndReimport();
                var clips = imp.defaultClipAnimations;
                foreach (var c in clips)
                {
                    string motion = Motions.FirstOrDefault(n => c.name.EndsWith(n, StringComparison.OrdinalIgnoreCase));
                    if (motion != null) c.name = motion;
                    c.loopTime = c.name == "Idle" || c.name == "Fly" || c.name == "Glide";
                    c.loopPose = c.loopTime;
                    c.lockRootRotation = true;
                    c.lockRootPositionXZ = true;
                    c.lockRootHeightY = true;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionXZ = true;
                    c.keepOriginalPositionY = true;
                }
                imp.clipAnimations = clips;
            }
            imp.SaveAndReimport();
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MainPath);
        var root = UnityEngine.Object.Instantiate(model);
        root.name = "LordLyrical_CyberDragon";
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var boneMap = root.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
        var lods = new LOD[3];
        var matCache = new Dictionary<string, Material>();
        var mainRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in mainRenderers) PrepareRenderer(r, matCache);
        lods[0] = new LOD(.55f, mainRenderers);
        for (int level = 1; level < 3; level++)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Models/LL_CyberDragon_LOD" + level + ".fbx");
            var instance = UnityEngine.Object.Instantiate(src);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var container = new GameObject("LOD" + level);
            container.transform.SetParent(root.transform, false);
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers)
            {
                r.bones = r.bones.Select(b => boneMap[b.name]).ToArray();
                if (r.rootBone != null) r.rootBone = boneMap[r.rootBone.name];
                r.transform.SetParent(container.transform, true);
                PrepareRenderer(r, matCache);
            }
            UnityEngine.Object.DestroyImmediate(instance);
            lods[level] = new LOD(level == 1 ? .25f : .04f, renderers);
        }
        var group = root.AddComponent<LODGroup>();
        group.SetLODs(lods); group.RecalculateBounds();
        var clipsAll = AssetDatabase.LoadAllAssetsAtPath(MainPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        string controllerPath = Folder + "/Animation/Dragon.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        else
        {
            var sm = controller.layers[0].stateMachine;
            foreach (var state in sm.states) sm.RemoveState(state.state);
            foreach (var trans in sm.anyStateTransitions) sm.RemoveAnyStateTransition(trans);
            controller.parameters = new AnimatorControllerParameter[0];
        }
        controller.AddParameter("Motion", AnimatorControllerParameterType.Int);
        var machine = controller.layers[0].stateMachine;
        var states = new Dictionary<string, AnimatorState>();
        for (int i = 0; i < Motions.Length; i++)
        {
            string n = Motions[i];
            var clip = clipsAll.FirstOrDefault(c => c.name == n);
            if (clip == null) throw new InvalidOperationException("Animation missing: " + n);
            var state = machine.AddState(n, new Vector3(250, 70 * i));
            state.motion = clip; state.writeDefaultValues = false; states[n] = state;
            var tr = machine.AddAnyStateTransition(state);
            tr.hasExitTime = false; tr.duration = .2f; tr.canTransitionToSelf = false;
            tr.AddCondition(AnimatorConditionMode.Equals, i, "Motion");
        }
        machine.defaultState = states["Idle"];
        var animator = root.GetComponent<Animator>() ?? root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(MainPath).OfType<Avatar>().FirstOrDefault();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Prefabs/LordLyrical_CyberDragon.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        Debug.Log("LORD_LYRICAL_DRAGON_READY: 3 LODs, 5 animation states, Generic rig.");
    }

    static void PrepareRenderer(SkinnedMeshRenderer renderer, Dictionary<string, Material> cache)
    {
        renderer.sharedMaterials = renderer.sharedMaterials.Select(m => GetMaterial(m.name, cache)).ToArray();
        renderer.quality = SkinQuality.Bone4;
        renderer.localBounds = new Bounds(new Vector3(0, 4, 0), new Vector3(22, 20, 22));
        renderer.updateWhenOffscreen = false;
    }

    static Material GetMaterial(string originalName, Dictionary<string, Material> cache)
    {
        string key = originalName.Split('.')[0].Replace(" (Instance)", "");
        if (cache.TryGetValue(key, out var found)) return found;
        bool urp = GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
        var shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null) throw new InvalidOperationException("Standard or URP Lit shader is required.");
        string safe = string.Concat(key.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        string path = Folder + "/Materials/" + safe + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        Color color = new Color(.53f,.57f,.62f); float metal = .82f, smooth = .72f; Color emission = Color.black;
        if (key.Contains("Edge polished")) {color=new Color(.76f,.81f,.87f);metal=.9f;smooth=.78f;}
        else if (key.Contains("Graphite")) {color=new Color(.027f,.037f,.048f);smooth=.66f;}
        else if (key.Contains("Dark brushed")) {color=new Color(.10f,.13f,.16f);smooth=.70f;}
        else if (key.Contains("Cobalt")) {color=new Color(.004f,.20f,.72f);metal=.35f;emission=color*3.5f;}
        else if (key.Contains("Pale blue")) {color=new Color(.04f,.52f,1f);metal=.2f;emission=color*6f;}
        else if (key.Contains("Wing fine")) {color=new Color(.009f,.055f,.18f);metal=.4f;emission=color*.8f;}
        else if (key.Contains("Wing carbon")) {color=Color.white;metal=.65f;smooth=.61f;emission=Color.white*1.7f;}
        mat.SetColor(urp ? "_BaseColor" : "_Color",color);
        mat.SetFloat("_Metallic",metal);mat.SetFloat(urp ? "_Smoothness" : "_Glossiness",smooth);
        if (emission.maxColorComponent>0) {mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",emission);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
        if (key.Contains("Wing carbon"))
        {
            mat.SetTexture(urp ? "_BaseMap" : "_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/LL_Wing_BaseColor.png"));
            mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/LL_Wing_Emission.png"));
        }
        EditorUtility.SetDirty(mat);cache[key]=mat;return mat;
    }
}
#endif
