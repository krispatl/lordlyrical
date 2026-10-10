using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using LordLyrical.Characters;

namespace LordLyrical
{
    // One small draw for soft floor contact. Skeletons remain the sole pose owners.
    public sealed class CharacterGroundContact : MonoBehaviour
    {
        sealed class Actor
        {
            public Transform root,left,right;
            public SonicOrcPerformance orc;
            public bool founder;
        }
        readonly List<Actor> actors=new();
        readonly List<Vector3> vertices=new(156);
        readonly List<Vector2> uvs=new(156);
        readonly List<Color32> colors=new(156);
        readonly List<int> triangles=new(234);
        Mesh mesh;
        MeshRenderer surface;
        public int VisiblePatches {get;private set;}
        public void Initialize(Transform world)
        {
            if(mesh)return;
            var founder=world.GetComponentInChildren<JeruPerformance>(true);
            if(founder&&founder.animator&&founder.animator.isHuman)
                actors.Add(new Actor{root=founder.transform,founder=true,
                    left=founder.animator.GetBoneTransform(HumanBodyBones.LeftFoot),
                    right=founder.animator.GetBoneTransform(HumanBodyBones.RightFoot)});
            foreach(var orc in world.GetComponentsInChildren<SonicOrcPerformance>(true)) {
                Transform left=null,right=null;
                foreach(var bone in orc.GetComponentsInChildren<Transform>(true)){
                    if(bone.name=="foot_l")left=bone;if(bone.name=="foot_r")right=bone;
                }
                actors.Add(new Actor{root=orc.transform,left=left,right=right,orc=orc});
            }
            mesh=new Mesh{name="Batched character floor contact"};mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            surface=gameObject.AddComponent<MeshRenderer>();
            surface.sharedMaterial=Resources.Load<Material>("Concert/CharacterGroundContact");
            surface.shadowCastingMode=ShadowCastingMode.Off;surface.receiveShadows=false;
            surface.lightProbeUsage=LightProbeUsage.Off;surface.reflectionProbeUsage=ReflectionProbeUsage.Off;
            surface.enabled=false;
        }
        public void Render(bool on)
        {
            if(!mesh)return;
            vertices.Clear();uvs.Clear();colors.Clear();triangles.Clear();VisiblePatches=0;
            if(on)foreach(var actor in actors) {
                if(!actor.root||!actor.root.gameObject.activeInHierarchy)continue;
                float alive=actor.orc?1-actor.orc.DissolveAmount:1;
                float height=Mathf.Abs(actor.root.position.y);
                float bodyAlpha=.16f*alive*(1-Mathf.SmoothStep(0,1,height/1.5f));
                Add(actor.root.position,actor.root.forward,actor.founder?.42f:.70f,actor.founder?.34f:.55f,bodyAlpha);
                Foot(actor.left,actor.founder,alive);Foot(actor.right,actor.founder,alive);
            }
            mesh.Clear(false);mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            surface.enabled=VisiblePatches>0;
        }
        void Foot(Transform foot,bool founder,float alive)
        {
            if(!foot)return;
            float height=Mathf.Max(0,foot.position.y-(founder?.08f:.15f));
            float alpha=.32f*alive*(1-Mathf.SmoothStep(0,1,height/.65f));
            // Broaden and soften as a foot lifts; planted feet retain a tight contact.
            Add(foot.position,foot.forward,(founder?.13f:.22f)+height*.16f,(founder?.24f:.34f)+height*.2f,alpha);
        }
        void Add(Vector3 point,Vector3 forward,float width,float length,float alpha)
        {
            if(alpha<.005f)return;
            var along=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
            if(along.sqrMagnitude<.1f)along=Vector3.forward;
            Vector3 side=Vector3.Cross(Vector3.up,along)*width;along*=length;
            point.y=.012f;int index=vertices.Count;Color32 color=new Color(1,1,1,alpha);
            vertices.Add(transform.InverseTransformPoint(point-side-along));
            vertices.Add(transform.InverseTransformPoint(point-side+along));
            vertices.Add(transform.InverseTransformPoint(point+side+along));
            vertices.Add(transform.InverseTransformPoint(point+side-along));
            uvs.Add(new Vector2(-1,-1));uvs.Add(new Vector2(-1,1));uvs.Add(new Vector2(1,1));uvs.Add(new Vector2(1,-1));
            for(int i=0;i<4;i++)colors.Add(color);
            triangles.Add(index);triangles.Add(index+1);triangles.Add(index+2);
            triangles.Add(index);triangles.Add(index+2);triangles.Add(index+3);VisiblePatches++;
        }
        void OnDestroy(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
