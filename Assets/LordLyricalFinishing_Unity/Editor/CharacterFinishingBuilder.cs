using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LordLyrical.Characters;

public static class CharacterFinishingBuilder
{
    const string Root="Assets/LordLyricalFinishing_Unity";
    static GameObject Import(string name){string path=Root+"/Models/"+name+".fbx";var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.animationType=ModelImporterAnimationType.Generic;imp.importAnimation=false;imp.importBlendShapes=true;imp.isReadable=false;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<GameObject>(path);}
    static SkinnedMeshRenderer Attach(SkinnedMeshRenderer src,SkinnedMeshRenderer original,Transform[] skeleton)
    {
        var g=new GameObject(src.name);g.transform.SetParent(original.transform.parent,false);g.transform.localPosition=src.transform.localPosition;g.transform.localRotation=src.transform.localRotation;g.transform.localScale=src.transform.localScale;
        var r=g.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=src.sharedMesh;
        r.bones=src.bones.Select(b=>skeleton.FirstOrDefault(t=>t.name==b.name)??throw new Exception("Missing distance binding "+b.name)).ToArray();r.rootBone=skeleton.First(t=>t.name==src.rootBone.name);r.localBounds=original.localBounds;r.quality=SkinQuality.Bone4;r.updateWhenOffscreen=true;
        r.sharedMaterials=src.sharedMaterials.Select(m=>original.sharedMaterials.FirstOrDefault(o=>o.name==m.name)??original.sharedMaterials[0]).ToArray();return r;
    }
    [MenuItem("Lord Lyrical/Finishing/Finish Character Detail and LODs")]
    public static void Build()
    {
        foreach(string p in new[]{"Woven_Brocade","Forged_Microdetail","Skin_Pores"}){var i=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/"+p+"_Normal.png");i.textureType=TextureImporterType.NormalMap;i.wrapMode=TextureWrapMode.Repeat;i.mipmapEnabled=true;i.maxTextureSize=512;i.SaveAndReimport();}
        foreach(string folder in new[]{"Assets/LordLyricalJeru_Unity/Materials","Assets/LordLyricalAvatars_Unity/Materials"})foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{folder})){
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(m.shader.name!="Universal Render Pipeline/Lit"||m.name.Contains("Eyes")||m.name.Contains("Cymatic")||m.name.Contains("Floor"))continue;
            string kind=m.name.Contains("Skin")||m.name.Contains("body")?"Skin_Pores":m.name.Contains("Armor")||m.name.Contains("Trim")||m.name.Contains("Silver")?"Forged_Microdetail":"Woven_Brocade";
            m.SetTexture("_DetailNormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+kind+"_Normal.png"));m.SetTextureScale("_DetailAlbedoMap",Vector2.one*(kind=="Skin_Pores"?2:6));m.SetFloat("_DetailNormalMapScale",kind=="Skin_Pores"?.15f:.35f);m.EnableKeyword("_DETAIL_SCALED");EditorUtility.SetDirty(m);
        }
        var config=AssetDatabase.LoadAssetAtPath<LordLyrical.ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");string path=AssetDatabase.GetAssetPath(config.lordModel);var model=Import("Jeru_Distance_Meshes");var root=PrefabUtility.LoadPrefabContents(path);
        try{
            foreach(var old in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.Contains("_LOD")).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var baseMeshes=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var skeleton=root.GetComponent<JeruPerformance>().animator.GetComponentsInChildren<Transform>(true);var lod=root.GetComponent<LODGroup>();if(!lod)lod=root.AddComponent<LODGroup>();
            var levels=new List<LOD>{new LOD(.34f,baseMeshes)};var sources=new List<SkinnedMeshRenderer>();var targets=new List<SkinnedMeshRenderer>();
            for(int level=1;level<=2;level++){
                var rs=new List<Renderer>();foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.EndsWith("_LOD"+level))){string name=skin.name.Substring(0,skin.name.Length-5);var original=baseMeshes.First(r=>r.name==name);var dst=Attach(skin,original,skeleton);rs.Add(dst);if(dst.sharedMesh.blendShapeCount>0){sources.Add(original);targets.Add(dst);}}
                levels.Add(new LOD(level==1?.15f:.003f,rs.ToArray()));
            }
            lod.SetLODs(levels.ToArray());lod.RecalculateBounds();var relay=root.GetComponent<FacialLodRelay>();if(!relay)relay=root.AddComponent<FacialLodRelay>();relay.sources=sources.ToArray();relay.targets=targets.ToArray();PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(string id in ParticipantAvatarBuilder.Ids){
            model=Import(id+"_Distance_Hands");path=ParticipantAvatarBuilder.Root+"/Prefabs/"+id+".prefab";root=PrefabUtility.LoadPrefabContents(path);
            try{
                var p=root.GetComponent<ParticipantAvatarPerformance>();foreach(var old in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.Contains("Hand_LOD")).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
                var skeleton=p.animator.GetComponentsInChildren<Transform>(true);var handGroup=root.transform.Find("Distance hand LOD controls");if(!handGroup){var g=new GameObject("Distance hand LOD controls");g.transform.SetParent(root.transform,false);handGroup=g.transform;}var lod=handGroup.GetComponent<LODGroup>();if(!lod)lod=handGroup.gameObject.AddComponent<LODGroup>();
                var levels=new List<LOD>{new LOD(.4f,new Renderer[]{p.leftHandMesh,p.rightHandMesh})};var extras=new List<SkinnedMeshRenderer>();
                for(int level=1;level<=2;level++){var rs=new List<Renderer>();foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.EndsWith("_LOD"+level))){var original=skin.name.Contains("Left")?p.leftHandMesh:p.rightHandMesh;var dst=Attach(skin,original,skeleton);dst.sharedMaterials=original.sharedMaterials;extras.Add(dst);rs.Add(dst);}levels.Add(new LOD(level==1?.13f:.003f,rs.ToArray()));}
                lod.SetLODs(levels.ToArray());lod.RecalculateBounds();p.handLod=lod;p.distanceHands=extras.ToArray();PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        // Refit JERU's animation envelope when distance meshes are regenerated.
        // Participant hands remain unrestricted because headset users control their reach.
        SkinCullingReview.Prepare();
        Debug.Log("CHARACTER_DETAIL_AND_LODS_READY");
    }
}
