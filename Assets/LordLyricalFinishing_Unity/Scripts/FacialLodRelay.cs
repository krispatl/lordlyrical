using System;
using UnityEngine;
namespace LordLyrical.Characters
{
    public sealed class FacialLodRelay:MonoBehaviour
    {
        public SkinnedMeshRenderer[] sources,targets;
        int[][] indices;
        public void Synchronize()
        {
            if(sources==null||targets==null)return;
            if(indices==null){indices=new int[targets.Length][];for(int n=0;n<targets.Length;n++){
                var s=sources[n].sharedMesh;var t=targets[n].sharedMesh;indices[n]=new int[t.blendShapeCount];
                for(int j=0;j<t.blendShapeCount;j++){indices[n][j]=-1;string name=t.GetBlendShapeName(j);name=name.Substring(name.LastIndexOf('.')+1);for(int k=0;k<s.blendShapeCount;k++){string sn=s.GetBlendShapeName(k);if(sn==name||sn.EndsWith("."+name,StringComparison.Ordinal)){indices[n][j]=k;break;}}}
            }}
            for(int n=0;n<targets.Length;n++)for(int j=0;j<indices[n].Length;j++)if(indices[n][j]>=0)targets[n].SetBlendShapeWeight(j,sources[n].GetBlendShapeWeight(indices[n][j]));
        }
        void LateUpdate(){Synchronize();}
    }
}
