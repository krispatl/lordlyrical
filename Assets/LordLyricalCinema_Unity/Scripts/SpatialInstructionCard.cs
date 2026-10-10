using UnityEngine;
namespace LordLyrical
{
    public sealed class SpatialInstructionCard : MonoBehaviour
    {
        Material material;Mesh mesh;
        public void Initialize(TextMesh text)
        {
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            text.GetComponent<MeshRenderer>().sortingOrder=40;
            text.characterSize=.006f;text.fontSize=80;text.color=new Color(.86f,.91f,.96f);
            mesh=new Mesh{name="Spatial instruction card",vertices=new[]{new Vector3(-.46f,-.16f,0),new Vector3(.46f,-.16f,0),new Vector3(-.46f,.16f,0),new Vector3(.46f,.16f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};mesh.RecalculateBounds();
            material=new Material(Resources.Load<Shader>("Cinema26/InstructionCard"));
            var go=new GameObject("Alignment | smoked glass and progress");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,-.012f,.008f);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public void Appearance(Color accent,float opacity,bool progress)
        {if(material){material.SetColor("_Accent",accent);material.SetFloat("_Opacity",opacity);material.SetFloat("_ProgressVisible",progress?1:0);}}
        public void Progress(float value){if(material)material.SetFloat("_Progress",value);}
        void OnDestroy(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}