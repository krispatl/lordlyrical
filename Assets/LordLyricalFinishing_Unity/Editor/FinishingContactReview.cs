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

public static class FinishingContactReview
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static Bounds Bounds(SkinnedMeshRenderer r){var mesh=new Mesh();r.BakeMesh(mesh);var points=mesh.vertices;var b=new Bounds(r.transform.TransformPoint(points[0]),Vector3.zero);foreach(var v in points)b.Encapsulate(r.transform.TransformPoint(v));UnityEngine.Object.DestroyImmediate(mesh);return b;}
    [MenuItem("Lord Lyrical/Finishing/Verify Character LODs and Contacts")]
    public static void Review()
    {
        var before=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);var report=new StringBuilder("CHARACTER DETAIL AND CONTACT REVIEW\n");
        try{
            var config=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");var session=new GameObject("Review session").AddComponent<NetworkSessionController>();var world=new GameObject("Contact world").AddComponent<ResonantWorld>();world.Initialize(config,session);world.Render(ShowFrame.At(31),true);
            var jeru=world.GetComponentInChildren<JeruPerformance>();Require(jeru.GetComponent<LODGroup>().lodCount==3,"Jeru LOD group");var relay=jeru.GetComponent<FacialLodRelay>();
            jeru.SampleClip(5,.7f);relay.Synchronize();for(int i=0;i<relay.targets.Length;i++){Require(relay.targets[i].sharedMesh.blendShapeCount==66,"Lost facial shapes");float diff=Vector3.Distance(Bounds(relay.targets[i]).center,Bounds(relay.sources[i]).center);Require(diff<.12f,"Jeru distance face binding offset "+diff);report.AppendLine($"PASS {relay.targets[i].name}: 66 facial shapes; world bounds centre delta {diff:F4}m");}
            foreach(var p in world.GetComponentsInChildren<ParticipantAvatarPerformance>(true)){
                Require(p.handLod&&p.handLod.lodCount==3&&p.distanceHands.Length==4,"Missing hand LODs");p.SampleClip(5,1);
                foreach(var r in p.distanceHands){var source=r.name.Contains("Left")?p.leftHandMesh:p.rightHandMesh;float delta=Vector3.Distance(Bounds(r).center,Bounds(source).center);Require(delta<.06f,"Hand binding offset "+p.roleName+" "+delta);}
                p.SetFirstPerson(true);Require(p.distanceHands.All(r=>!r.enabled)&&!p.handLod.enabled&&p.leftHandMesh.enabled,"First-person LOD isolation");p.SetFirstPerson(false);report.AppendLine("PASS "+p.roleName+": 3 hand LODs, articulated binding and first-person isolation");
            }
            var camera=new GameObject("Contact camera").AddComponent<Camera>();camera.cullingMask=1<<27;camera.fieldOfView=43;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.012f,.018f,.027f);
            var key=new GameObject("Contact key").AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(35,-25,0);key.intensity=1.8f;key.cullingMask=1<<27;
            foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=27;
            foreach(float time in new[]{27f,31f,45.8f,47.8f}){world.Render(ShowFrame.At(time),true);camera.transform.position=new Vector3(-7,3.1f,10);camera.transform.LookAt(new Vector3(-3,1.5f,16-(time-26)*.14f));ParticipantAvatarBuilder.Render(camera,FinishingBuilder.Output+"/Orc_Contact_"+time.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+".png",1300,1000,0);}
            world.Render(ShowFrame.At(94),true);foreach(var text in world.GetComponentsInChildren<TextMesh>())text.gameObject.SetActive(false);camera.transform.position=new Vector3(1.6f,2.1f,1.9f);camera.transform.LookAt(new Vector3(0,1.5f,5));ParticipantAvatarBuilder.Render(camera,FinishingBuilder.Output+"/Jeru_Finished_Detail.png",1100,1300,0);
            File.WriteAllText(FinishingBuilder.Output+"/Character_LOD_Validation.txt",report.ToString());Debug.Log("CHARACTER_LOD_CONTACT_PASS\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(before);}
    }
}
