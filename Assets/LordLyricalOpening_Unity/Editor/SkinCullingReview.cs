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

public static class SkinCullingReview
{
    const string Out=MotionPolishValidation.Out+"/Production/SkinCulling";
    static Bounds TransformBounds(Matrix4x4 matrix,Bounds bounds)
    {
        Bounds result=new Bounds(matrix.MultiplyPoint3x4(bounds.center),Vector3.zero);
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            result.Encapsulate(matrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));
        return result;
    }
    [MenuItem("Lord Lyrical/Production/Inspect Skin Bounds")]
    public static void Inspect()
    {
        Directory.CreateDirectory(Out);var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("SKIN BOUNDS COORDINATE AUDIT\n");
        try{
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LordLyricalJeru_Unity/Prefabs/LordLyrical_Jeru.prefab");
            var actor=UnityEngine.Object.Instantiate(prefab);actor.transform.SetPositionAndRotation(new Vector3(3,0,7),Quaternion.Euler(0,35,0));
            var performance=actor.GetComponent<JeruPerformance>();performance.SampleShow(94);
            var targets=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.updateWhenOffscreen).ToArray();
            report.AppendLine($"Offscreen-updated meshes: {targets.Length}; vertices: {targets.Sum(r=>r.sharedMesh.vertexCount)}");
            foreach(var skin in targets.Take(4)){
                var baked=new Mesh();skin.BakeMesh(baked);
                var local=skin.localBounds;skin.updateWhenOffscreen=false;
                var probe=new Bounds(new Vector3(3,5,7),new Vector3(.74f,1.42f,.58f));skin.localBounds=probe;
                var byRenderer=TransformBounds(skin.localToWorldMatrix,probe);
                var byTransform=TransformBounds(skin.transform.localToWorldMatrix,probe);
                var byRoot=TransformBounds(skin.rootBone.localToWorldMatrix,probe);
                report.AppendLine($"{skin.name}; root={skin.rootBone.name}; object={skin.transform.position}; baked={baked.bounds}; saved={local}");
                report.AppendLine($"  Native bounds={skin.bounds}; renderer delta={Vector3.Distance(byRenderer.center,skin.bounds.center):F6}; transform delta={Vector3.Distance(byTransform.center,skin.bounds.center):F6}; root delta={Vector3.Distance(byRoot.center,skin.bounds.center):F6}");
                var projected=new Bounds();bool first=true;
                var matrix=skin.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                foreach(var vertex in baked.vertices){var v=matrix.MultiplyPoint3x4(vertex);if(first){projected=new Bounds(v,Vector3.zero);first=false;}else projected.Encapsulate(v);}
                skin.localBounds=projected;int outside=0;var bounds=skin.bounds;bounds.Expand(.001f);
                foreach(var vertex in baked.vertices)if(!bounds.Contains(skin.transform.TransformPoint(vertex)))outside++;
                report.AppendLine($"  Renderer-space fitted bounds: {projected}; outside world bounds: {outside}");
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        File.WriteAllText(Out+"/Coordinate_Audit.txt",report.ToString());Debug.Log("SKIN_COORDINATE_AUDIT_COMPLETE");
    }

    const string Prefab="Assets/LordLyricalJeru_Unity/Prefabs/LordLyrical_Jeru.prefab";
    static readonly System.Collections.Generic.List<Vector3> boundPoints=new(20000);
    static Bounds Fit(SkinnedMeshRenderer skin,Mesh scratch)
    {
        skin.BakeMesh(scratch,true);
        scratch.GetVertices(boundPoints);var points=boundPoints;var matrix=skin.rootBone.worldToLocalMatrix*skin.transform.localToWorldMatrix;
        var result=new Bounds(matrix.MultiplyPoint3x4(points[0]),Vector3.zero);
        for(int i=1;i<points.Count;i++)result.Encapsulate(matrix.MultiplyPoint3x4(points[i]));
        return result;
    }
    [MenuItem("Lord Lyrical/Production/Prepare JERU Skin Bounds")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Out);
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("JERU SKINNING BOUNDS PREPARATION\n");
        try {
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            actor.transform.localScale=Vector3.one*1.22f;
            actor.transform.position=ConductorRoute.Position(0);
            var performance=actor.GetComponent<JeruPerformance>();performance.SampleShow(0);
            var targets=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x=>x.name.EndsWith("_LOD1")||x.name.EndsWith("_LOD2")).ToArray();
            if(targets.Length!=28)throw new Exception("Expected 28 existing distance meshes");
            var fitted=targets.ToDictionary(x=>x.name,x=>new Bounds());
            var scratch=new Mesh();bool first=true;int samples=0;
            try {
                void Accumulate(){
                    foreach(var skin in targets){
                        var next=Fit(skin,scratch);var old=fitted[skin.name];
                        if(first)old=next;else old.Encapsulate(next);
                        fitted[skin.name]=old;
                    }
                    first=false;samples++;
                }
                // Include deterministic walking, all authored gestures and the landing.
                for(int i=0;i<=121*72;i++){float t=i/72f;actor.transform.position=ConductorRoute.Position(t);actor.transform.rotation=ConductorRoute.Rotation(t);performance.SampleShow(t);Accumulate();}
                for(int i=0;i<=48;i++){performance.SampleArrival(-2+i/24f);Accumulate();}
                // Protect extreme facial expressions as well as normal speech.
                performance.SampleShow(94);
                for(int shape=0;shape<66;shape++){
                    foreach(var skin in targets)for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)skin.SetBlendShapeWeight(i,i==shape?100:0);
                    Accumulate();
                }
                foreach(var skin in targets)for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)skin.SetBlendShapeWeight(i,100);
                Accumulate();
            }finally{UnityEngine.Object.DestroyImmediate(scratch);}
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try {
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(fitted.TryGetValue(skin.name,out var bounds)){
                    bounds.Expand(.30f);skin.updateWhenOffscreen=false;skin.localBounds=bounds;
                    report.AppendLine($"{skin.name}: vertices={skin.sharedMesh.vertexCount}; bounds={bounds}");
                    fitted[skin.name]=bounds;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            root=PrefabUtility.LoadPrefabContents(Prefab);
            try {
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(fitted.TryGetValue(skin.name,out var expected)){
                    if(skin.updateWhenOffscreen||Vector3.Distance(skin.localBounds.center,expected.center)>.0001f||Vector3.Distance(skin.localBounds.size,expected.size)>.0001f)
                        throw new Exception("Saved skin bounds did not persist: "+skin.name);
                }
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            report.AppendLine($"PASS {samples} source poses; 28 bounds saved and reloaded; padding 0.15m per side. Dense independent containment and render comparison still required.");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        File.WriteAllText(Out+"/Preparation.txt",report.ToString());Debug.Log("JERU_SKIN_BOUNDS_PREPARED");
    }

    [MenuItem("Lord Lyrical/Production/Validate JERU Skin Culling")]
    public static void Validate()
    {
        Directory.CreateDirectory(Out);
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("JERU SKIN CULLING VALIDATION / ACTUAL 1.22 SHOW SCALE, SCALE-COMPENSATED BAKE\n");
        try{
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var reference=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LordLyricalOpening_Unity/Editor/ReferenceJeruBeforeSkinning.prefab"));
            var performance=actor.GetComponent<JeruPerformance>();
            var oldPerformance=reference.GetComponent<JeruPerformance>();
            actor.transform.localScale=Vector3.one*1.22f;
            actor.transform.position=ConductorRoute.Position(0);
            actor.transform.localScale=reference.transform.localScale=Vector3.one*1.22f;
            performance.SampleShow(0);oldPerformance.SampleShow(0);
            var targets=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x=>x.name.EndsWith("_LOD1")||x.name.EndsWith("_LOD2")).ToArray();
            var original=reference.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToDictionary(x=>x.name);
            if(targets.Length!=28||targets.Any(x=>x.updateWhenOffscreen))throw new Exception("Offscreen updates remain");
            foreach(var skin in targets)if(skin.sharedMesh!=original[skin.name].sharedMesh||!skin.sharedMaterials.SequenceEqual(original[skin.name].sharedMaterials))throw new Exception("Authored mesh or material changed");
            var mesh=new Mesh();var points=new System.Collections.Generic.List<Vector3>(20000);
            long vertices=0;int frames=0;float minimumClearance=float.MaxValue;int failures=0;
            try{
                for(int frame=0;frame<=121*90;frame++){
                    float t=frame/90f;
                    actor.transform.position=ConductorRoute.Position(t);actor.transform.rotation=ConductorRoute.Rotation(t);
                    performance.SampleShow(t);
                    foreach(var skin in targets){
                        skin.BakeMesh(mesh,true);mesh.GetVertices(points);
                        var matrix=skin.rootBone.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                        var limit=skin.localBounds;var lo=limit.min;var hi=limit.max;
                        for(int i=0;i<points.Count;i++){
                            var v=matrix.MultiplyPoint3x4(points[i]);
                            float clearance=Mathf.Min(Mathf.Min(v.x-lo.x,hi.x-v.x),Mathf.Min(Mathf.Min(v.y-lo.y,hi.y-v.y),Mathf.Min(v.z-lo.z,hi.z-v.z)));
                            minimumClearance=Mathf.Min(minimumClearance,clearance);vertices++;
                            if(!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)||clearance<-.0001f){
                                failures++;if(failures<5)report.AppendLine($"OUTSIDE {skin.name} t={t:F4} v={v} limit={limit}");
                            }
                        }
                        // The native world bound must use the same root-bone coordinate frame.
                        var native=skin.bounds;var expected=TransformBounds(skin.rootBone.localToWorldMatrix,limit);
                        if(Vector3.Distance(native.center,expected.center)>.001f||Vector3.Distance(native.size,expected.size)>.001f)
                            throw new Exception("Native bound transform mismatch: "+skin.name);
                    }
                    frames++;
                }
            }finally{UnityEngine.Object.DestroyImmediate(mesh);}
            report.AppendLine($"Sampled {frames} full-show poses at90Hz (independent of the72Hz bound preparation) / {vertices} distance-mesh vertices. Outside bounds: {failures}. Minimum local clearance={minimumClearance:F4}m.");
            if(failures>0)throw new Exception("Dense skin containment failed: "+failures);
            report.AppendLine("PASS independent animated containment; rendering comparisons run on separate editor frames below.");
            File.WriteAllText(Out+"/Containment.txt",report.ToString());
        }catch(Exception error){File.WriteAllText(Out+"/Validation_Failed.txt",report+"\n"+error);throw;}
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
        BeginRenderValidation();
    }

    static bool renderRunning;
    const string ReviewSession="LordLyrical.SkinRendering";
    const string ReviewScene="Assets/LordLyricalOpening_Unity/Editor/SkinCullingTemporaryReview.unity";
    [Serializable] class SavedAsset { public string guid; public long localId; }
    [Serializable] class SavedScenes { public SceneSetup[] scenes; public SavedAsset[] preloads; }
    static SavedAsset Identify(UnityEngine.Object asset)
    {
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long localId);
        return new SavedAsset{guid=guid,localId=localId};
    }
    static UnityEngine.Object Resolve(SavedAsset id)
    {
        return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(id.guid)).First(asset=>{
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long localId);
            return guid==id.guid&&localId==id.localId;
        });
    }
    [InitializeOnLoadMethod] static void InstallRenderHook()
    {
        EditorApplication.playModeStateChanged-=ReviewPlayModeChanged;
        EditorApplication.playModeStateChanged+=ReviewPlayModeChanged;
    }
    static void ReviewPlayModeChanged(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(ReviewSession,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){
            renderRunning=true;
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(RenderValidationGuard());
        }
        if(state==PlayModeStateChange.EnteredEditMode){
            SessionState.SetBool(ReviewSession,false);renderRunning=false;
            var saved=JsonUtility.FromJson<SavedScenes>(SessionState.GetString(ReviewSession+".scenes",""));
            if(saved?.scenes!=null)EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
            // XR editor teardown can clear the build's preloaded settings. Preserve them.
            if(saved?.preloads!=null)PlayerSettings.SetPreloadedAssets(saved.preloads.Select(Resolve).ToArray());
            AssetDatabase.DeleteAsset(ReviewScene);
        }
    }
    [MenuItem("Lord Lyrical/Production/Validate JERU Skin Rendering")]
    public static void BeginRenderValidation()
    {
        if(renderRunning||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Skin render validation requires stopped editor");
        if(!File.Exists(Out+"/Containment.txt"))throw new Exception("Run dense containment first");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save existing scene changes before review");
        if(File.Exists(Out+"/Validation.txt"))File.Delete(Out+"/Validation.txt");
        SessionState.SetString(ReviewSession+".scenes",JsonUtility.ToJson(new SavedScenes{scenes=EditorSceneManager.GetSceneManagerSetup(),preloads=PlayerSettings.GetPreloadedAssets().Select(Identify).ToArray()}));
        var empty=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorSceneManager.SaveScene(empty,ReviewScene);
        SessionState.SetBool(ReviewSession,true);renderRunning=true;
        EditorApplication.isPlaying=true;
    }
    static System.Collections.IEnumerator RenderValidationGuard()
    {
        var routine=RenderValidation();
        try {
            while(true){
                bool more;
                try{more=routine.MoveNext();}
                catch(Exception error){File.WriteAllText(Out+"/Rendering_Failed.txt",error.ToString());throw;}
                if(!more)break;
                yield return routine.Current;
            }
        }finally{(routine as IDisposable)?.Dispose();renderRunning=false;EditorApplication.isPlaying=false;}
    }
    static System.Collections.IEnumerator RenderValidation()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.CreateScene("Isolated JERU skin review");SceneManager.SetActiveScene(scene);
        var report=new StringBuilder(File.ReadAllText(Out+"/Containment.txt"));
        try{
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var reference=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LordLyricalOpening_Unity/Editor/ReferenceJeruBeforeSkinning.prefab"));
            var performance=actor.GetComponent<JeruPerformance>();var oldPerformance=reference.GetComponent<JeruPerformance>();
            actor.transform.localScale=reference.transform.localScale=Vector3.one*1.22f;
            performance.SampleShow(0);oldPerformance.SampleShow(0);
            foreach(var tr in actor.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=27;
            foreach(var tr in reference.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
            var camera=new GameObject("Native skin review").AddComponent<Camera>();camera.fieldOfView=48;camera.nearClipPlane=.08f;camera.farClipPlane=50;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.006f,.009f,.016f);
            camera.transform.position=new Vector3(.45f,1.4f,-3.0f);camera.transform.LookAt(new Vector3(0,1.12f,0));
            var key=new GameObject("Review key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(30,-25,0);key.cullingMask=(1<<27)|(1<<28);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.14f,.18f);
            int comparisons=0;var previousPose=new System.Collections.Generic.Dictionary<int,Color32[]>();
            foreach(float time in new[]{24.3f,73.2f,94.5f,107f})foreach(int lod in new[]{1,2}){
                for(int i=0;i<18;i++){float t=time-.25f+i/72f;performance.SampleShow(t);oldPerformance.SampleShow(t);}
                performance.SampleShow(time);oldPerformance.SampleShow(time);
                actor.GetComponent<LODGroup>().ForceLOD(lod);reference.GetComponent<LODGroup>().ForceLOD(lod);
                // Unity uploads a skinned pose once per player-loop frame. Camera.Render alone
                // repeatedly in one editor callback can otherwise return the first pose.
                for(int warm=0;warm<3;warm++){
                    EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();
                    yield return null;
                }
                Color32[] Capture(int layer,string name){
                    camera.cullingMask=1<<layer;var before=RenderTexture.active;var target=camera.targetTexture;
                    var rt=new RenderTexture(768,1024,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(768,1024,TextureFormat.RGBA32,false);
                    bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
                    try{
                        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                        texture.ReadPixels(new Rect(0,0,768,1024),0,0);texture.Apply();
                        File.WriteAllBytes(Out+"/"+name+".png",texture.EncodeToPNG());return texture.GetPixels32();
                    }finally{camera.targetTexture=target;RenderTexture.active=before;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);ShaderUtil.allowAsyncCompilation=async;}
                }
                string prefix=time.ToString("000.0",System.Globalization.CultureInfo.InvariantCulture)+"_LOD"+lod;
                var a=Capture(28,prefix+"_before");var b=Capture(27,prefix+"_after");
                int difference=0,foreground=0,motionPixels=0;long total=0;
                previousPose.TryGetValue(lod,out var last);
                for(int i=0;i<a.Length;i++){
                    int d=Math.Max(Math.Abs(a[i].r-b[i].r),Math.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));
                    difference=Math.Max(difference,d);total+=d;if(a[i].r>30||a[i].g>30||a[i].b>30)foreground++;
                    if(last!=null&&Math.Max(Math.Abs(last[i].r-b[i].r),Math.Max(Math.Abs(last[i].g-b[i].g),Math.Abs(last[i].b-b[i].b)))>8)motionPixels++;
                }
                if(foreground<500)throw new Exception("Skin capture unexpectedly empty: "+prefix);
                report.AppendLine($"Actual skinned capture {prefix}: maximum channel difference={difference}, mean maximum-channel difference={(double)total/a.Length:F6}, foreground pixels={foreground}, changed pixels from prior pose={(last==null?-1:motionPixels)}, play frame={Time.frameCount}.");
                File.WriteAllText(Out+"/Rendering_Progress.txt",report.ToString());
                if(difference>1)throw new Exception("Skin appearance differs: "+prefix);
                if(last!=null&&motionPixels<200)throw new Exception("Skin pose did not visibly advance: "+prefix);
                previousPose[lod]=b;comparisons++;
            }
            report.AppendLine($"PASS {comparisons} actual SkinnedMeshRenderer capture comparisons across separate play-mode frames (no baked-mesh replacement); visible pose changes verified. Original geometry, materials and animation drivers unchanged. No native headset FPS claim.");
            File.WriteAllText(Out+"/Validation.txt",report.ToString());Debug.Log("JERU_SKIN_CULLING_PASS");
        }finally{SceneManager.SetActiveScene(previous);SceneManager.UnloadSceneAsync(scene);}
    }
}
