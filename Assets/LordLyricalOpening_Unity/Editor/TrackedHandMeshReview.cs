using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Hands;
using LordLyrical;

public static class TrackedHandMeshReview
{
    const string Root = "Assets/LordLyricalTrackedHands_Unity";
    const string ResourceRoot = Root + "/Resources/LordLyrical";
    const string Output = @"C:\Users\kris\Documents\Codex\2026-10-03\bui\outputs\LordLyrical_Animation_Polish\Production\TrackedHands22";
    static void Require(bool ok, string error) { if (!ok) throw new Exception(error); }

    [MenuItem("Lord Lyrical/Production/Build Anatomical Tracked Hands")]
    public static void Build()
    {
        Directory.CreateDirectory(ResourceRoot);
        AssetDatabase.Refresh();
        string materialPath = Root + "/TrackedHandSurface.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
        material.SetColor("_BaseColor", new Color(.38f,.43f,.47f,1));
        material.SetFloat("_Metallic", .08f); material.SetFloat("_Smoothness", .34f);
        material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",new Color(.008f,.012f,.016f));
        foreach (bool left in new[]{true,false})
        {
            string side = left ? "Left" : "Right";
            string path = Root + "/Models/" + side + "Hand.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation = false; importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                go.name = side + " anatomical tracked hand";
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
                foreach (var animator in go.GetComponentsInChildren<Animator>()) UnityEngine.Object.DestroyImmediate(animator);
                var driver = go.AddComponent<TrackedHandMesh>(); driver.left = left;
                driver.skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
                Require(driver.skin, "Missing hand skin");
                var all = go.GetComponentsInChildren<Transform>(true);
                driver.jointIds = Enumerable.Range(0,XRHandJointID.EndMarker.ToIndex()).Select(XRHandJointIDUtility.FromIndex).ToArray();
                driver.joints = driver.jointIds.Select(id => all.Single(t => t.name.StartsWith(id.ToString(),StringComparison.OrdinalIgnoreCase) || t.name.EndsWith(id.ToString(),StringComparison.OrdinalIgnoreCase))).ToArray();
                Require(driver.joints.Length == 26 && driver.jointIds[0] == XRHandJointID.Wrist, "Expected26 joints with wrist first");
                driver.skin.sharedMaterial = material;
                driver.skin.rootBone = driver.joints[0];
                driver.skin.updateWhenOffscreen = true;
                driver.skin.localBounds = new Bounds(Vector3.zero, Vector3.one*.9f);
                driver.skin.shadowCastingMode = ShadowCastingMode.Off;
                driver.skin.receiveShadows = true;
                driver.skin.quality = SkinQuality.Bone4;
                driver.skin.enabled = false;
                PrefabUtility.SaveAsPrefabAsset(go, ResourceRoot + "/Tracked" + side + "Hand.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
        Debug.Log("LL_ANATOMICAL_HANDS_BUILT");
    }

    static Pose[] Poses(TrackedHandMesh hand) => hand.joints.Select(t => new Pose(t.position,t.rotation)).ToArray();
    static void Capture(Camera camera, TrackedHandMesh hand, string file)
    {
        var old=RenderTexture.active;var oldTarget=camera.targetTexture;
        var rt=new RenderTexture(1100,850,24,RenderTextureFormat.ARGB32);
        var mesh=new Mesh();hand.skin.BakeMesh(mesh,true);var snapshot=new GameObject("Baked tracked hand");snapshot.transform.SetParent(hand.skin.transform,false);snapshot.AddComponent<MeshFilter>().sharedMesh=mesh;snapshot.AddComponent<MeshRenderer>().sharedMaterials=hand.skin.sharedMaterials;hand.skin.enabled=false;
        Texture2D tex=null;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try { camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex=new Texture2D(1100,850,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,1100,850),0,0);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(snapshot);UnityEngine.Object.DestroyImmediate(mesh);hand.skin.enabled=true;camera.targetTexture=oldTarget;RenderTexture.active=old;ShaderUtil.allowAsyncCompilation=async;UnityEngine.Object.DestroyImmediate(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex); }
    }

    [MenuItem("Lord Lyrical/Production/Validate Anatomical Tracked Hands")]
    public static void Validate()
    {
        Directory.CreateDirectory(Output);
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("ANATOMICAL XR HAND MESH VALIDATION - CANDIDATE22\n");
        float maxError=0,maxCovariance=0;int cases=0;
        try
        {
            var camera=new GameObject("Hand review camera").AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.027f,.035f,.05f);
            camera.orthographic=true;camera.orthographicSize=.20f;camera.nearClipPlane=.01f;camera.farClipPlane=4;
            var light=new GameObject("Hand review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2.3f;light.transform.rotation=Quaternion.Euler(45,-35,0);
            var fill=new GameObject("Hand review fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;fill.transform.rotation=Quaternion.Euler(20,130,0);
            foreach(bool left in new[]{true,false})
            {
                string side=left?"Left":"Right";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ResourceRoot+"/Tracked"+side+"Hand.prefab");
                Require(prefab,"Missing tracked hand prefab");
                var hand=UnityEngine.Object.Instantiate(prefab).GetComponent<TrackedHandMesh>();
                var rest=Poses(hand);
                report.AppendLine(side+" mesh: vertices="+hand.skin.sharedMesh.vertexCount+" triangles="+hand.skin.sharedMesh.triangles.Length/3+" bones="+hand.skin.bones.Length);
                for(int variant=0;variant<6;variant++)
                {
                    // Anatomical fixture from the mesh's OpenXR bind skeleton:
                    // open, fist, point, splay, smaller and larger hand proportions.
                    hand.ApplyJointPoses(rest,true);
                    string poseName=new[]{"Open","Fist","Point","Splay","Small","Large"}[variant];
                    for(int i=0;i<hand.jointIds.Length;i++)
                    {
                        string id=hand.jointIds[i].ToString();
                        bool thumb=id.StartsWith("Thumb"),index=id.StartsWith("Index");
                        bool curl=variant==1||(variant==2&&!index&&!thumb);
                        if(curl&&!thumb&&(id.EndsWith("Proximal")||id.EndsWith("Intermediate")||id.EndsWith("Distal")))
                            hand.joints[i].localRotation *= Quaternion.Euler(id.EndsWith("Proximal")?65:id.EndsWith("Intermediate")?85:45,0,0);
                        if(variant==1&&thumb&&id.EndsWith("Proximal"))hand.joints[i].localRotation*=Quaternion.Euler(30,left?25:-25,0);
                        if(variant==3&&id.EndsWith("Proximal")&&!thumb)
                        {
                            float angle=id.StartsWith("Index")?-12:id.StartsWith("Middle")?-3:id.StartsWith("Ring")?6:16;
                            hand.joints[i].localRotation*=Quaternion.Euler(0,angle*(left?1:-1),0);
                        }
                    }
                    var poses=Poses(hand);
                    float scale=variant==4?.8f:variant==5?1.2f:1;
                    var origin=poses[0].position;
                    // Put the fixture in the viewer's space without any avatar body.
                    Quaternion rotation=Quaternion.Euler(0,left?-12:12,0);
                    for(int i=0;i<poses.Length;i++)poses[i]=new Pose(rotation*((poses[i].position-origin)*scale),rotation*poses[i].rotation);
                    hand.ApplyJointPoses(poses,true);
                    Require(hand.Visible&&hand.AppliedJointCount==26,"Valid frame hidden");
                    maxError=Mathf.Max(maxError,hand.JointPositionError);
                    var baked=new Mesh();hand.skin.BakeMesh(baked,true);
                    var points=baked.vertices.Select(v=>hand.skin.transform.TransformPoint(v)).ToArray();
                    Require(points.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"Nonfinite hand vertex");
                    var bounds=new Bounds(points[0],Vector3.zero);foreach(var v in points)bounds.Encapsulate(v);
                    Require(bounds.size.magnitude<.5f&&bounds.size.magnitude>.05f,"Hand stretched outside anatomical scale");
                    camera.transform.position=bounds.center+new Vector3(0,.40f,-.30f);
                    camera.transform.LookAt(bounds.center,Vector3.forward);
                    Capture(camera,hand,Output+"/"+side+"_"+poseName+".png");
                    Vector3 offset=new Vector3(1.3f,.9f,-.4f);Quaternion q=Quaternion.Euler(36,113,-27);
                    var shifted=poses.Select(x=>new Pose(q*x.position+offset,q*x.rotation)).ToArray();
                    hand.ApplyJointPoses(shifted,true);hand.skin.BakeMesh(baked,true);
                    var moved=baked.vertices.Select(v=>hand.skin.transform.TransformPoint(v)).ToArray();
                    float covariance=points.Zip(moved,(a,b)=>Vector3.Distance(q*a+offset,b)).Max();
                    maxCovariance=Mathf.Max(maxCovariance,covariance);
                    Require(covariance<.0002f,"Tracking-origin rotation/translation distorted hand");
                    UnityEngine.Object.DestroyImmediate(baked);cases++;
                    report.AppendLine(side+" "+poseName+": all26 joints matched; bounds="+bounds.size+" rigid transform error="+covariance);
                }
                hand.ApplyJointPoses(rest,false);Require(!hand.Visible,"Lost tracking leaves mesh visible");
                var invalid=(Pose[])rest.Clone();invalid[5].position=new Vector3(float.NaN,0,0);
                hand.ApplyJointPoses(invalid,true);Require(!hand.Visible,"Invalid joint leaves mesh visible");
                hand.ApplyJointPoses(rest,true);Require(hand.Visible,"Tracking recovery failed");
                UnityEngine.Object.DestroyImmediate(hand.gameObject);
            }
            Require(maxError<.0001f,"Joint mismatch");
            report.AppendLine("PASS cases="+cases+" maximum joint position error metres="+maxError+" maximum rigid-motion skin error metres="+maxCovariance);
            report.AppendLine("Scope: synthetic anatomical tracking fixtures, not real-sensor accuracy or wearer acceptance. No camera imagery or biometric recording.");
            File.WriteAllText(Output+"/Validation.txt",report.ToString());
            Debug.Log("LL_ANATOMICAL_HANDS_PASS\n"+report);
        }
        finally { EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous); }
    }
}
