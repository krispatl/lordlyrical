using UnityEngine;
using UnityEditor;
using CrazyMinnow.SALSA;
using CrazyMinnow.SALSA.OneClicks;
using LordLyrical;

public sealed class JeruSalsaSetup : OneClickBase
{
    [MenuItem("Lord Lyrical/Concert/Configure JERU SALSA")]
    public static void Build()
    {
        const string path="Assets/LordLyricalJeru_Unity/Prefabs/LordLyrical_Jeru.prefab";
        var go=PrefabUtility.LoadPrefabContents(path);
        try{
            Init();NewConfiguration(OneClickConfiguration.ConfigType.Salsa);AddSmrSearch(".*");
            autoAdjustAnalysis=false;autoAdjustMicrophone=false;audioUpdateDelay=.035f;
            loCutoff=.04f;hiCutoff=.9f;useAdvDyn=true;advDynPrimaryBias=.65f;useAdvDynJitter=false;advDynSecondaryMix=.15f;
            foreach(string shape in new[]{"viseme_PP","viseme_ou","viseme_E","viseme_aa"}){
                NewExpression(shape);AddShapeComponent(new[]{shape},.065f,0,.075f,shape,.68f,true);
            }
            DoOneClickiness(go);ConfigureSalsaAudioSource(go,null,false);AddQueueProcessor(go);
            var face=go.GetComponent<JeruConcertFace>();if(!face)face=go.AddComponent<JeruConcertFace>();face.salsa=go.GetComponent<Salsa>();
            PrefabUtility.SaveAsPrefabAsset(go,path);Debug.Log("JERU_SALSA_CONFIGURED expressions="+face.salsa.visemes.Count);
        }finally{PrefabUtility.UnloadPrefabContents(go);}
    }
}
