using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical.Editor
{
    public static class ChamberImporter
    {
        [MenuItem("Lord Lyrical/Import Chamber Derivative")]
        public static void Import()
        {
            const string input="ArtSources/chamber-surfaces.bytes";
            using var reader=new BinaryReader(File.OpenRead(input));int count=reader.ReadInt32();
            if(count<1||count>200000)throw new InvalidDataException("Unexpected chamber surfel count");
            var vertices=new Vector3[count*4];var colors=new Color32[count*4];var uv=new Vector2[count*4];var indices=new int[count*6];
            for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
            for(int i=0;i<colors.Length;i++)colors[i]=new Color32(reader.ReadByte(),reader.ReadByte(),reader.ReadByte(),reader.ReadByte());
            for(int i=0;i<count;i++){int v=i*4,t=i*6;uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.up;uv[v+3]=Vector2.one;indices[t]=v;indices[t+1]=v+2;indices[t+2]=v+1;indices[t+3]=v+1;indices[t+4]=v+2;indices[t+5]=v+3;}
            var path="Assets/LordLyrical/Resources/ReferenceChamber.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh{name="Supplied chamber - 110k surfels",indexFormat=IndexFormat.UInt32};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.indexFormat=IndexFormat.UInt32;mesh.vertices=vertices;mesh.colors32=colors;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
            Debug.Log($"LL source chamber imported: {count} surfels, {count*2} triangles");
        }
    }
}
