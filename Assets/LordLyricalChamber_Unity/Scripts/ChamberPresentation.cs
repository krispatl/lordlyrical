using System;
using System.Linq;
using UnityEngine;

namespace LordLyrical
{
    [ExecuteAlways]
    public sealed class ChamberPresentation : MonoBehaviour
    {
        [Header("Preserved dragon asset")]
        public GameObject dragonPrefab;
        public AnimationClip unfoldClip, glideClip, flyClip, roarClip;
        [Header("Authoring preview")]
        public bool previewInEditor;
        [Range(21,121)] public float previewTime=21;
        public bool showDragon=true;
        public Vector3 dragonPosition=new Vector3(0,3.6f,11);
        public float dragonScale=1.35f;
        [Header("Animated glyph materials")]
        [Tooltip("Disable to animate the material properties directly with your own Unity animation or Timeline.")]
        public bool driveMaterialsFromShow=true;
        [Range(0,4)] public float glyphIntensityMultiplier=1;
        [Range(.1f,4)] public float glyphPulseSpeed=1;
        public Color glyphColour=new Color(.015f,.48f,1);

        public ConcertState Battle;
        Renderer[] surfaces;
        Transform[] brokenColumns, revealRibs;
        Quaternion[] columnRest,ribRest;
        Vector3[] columnPositions;
        Transform well,dragon,head,chest;
        DragonBreach breach;
        Renderer[] dragonRenderers;
        Material[][] dragonOriginals;
        Material[][] dragonInstances;
        Color[][] baseColours,emissionColours;
        Animator animator;
        DragonPerformance performance;
        Transform[] poseNodes;
        Vector3[] restPositions,restScales;
        Quaternion[] restRotations;
        MaterialPropertyBlock block;
        bool initialized,impactCached,afterImpact;
        Vector3 impactPoint;
        bool materialsWereDriven;
        public Vector3 Mouth => performance!=null?performance.Mouth:transform.TransformPoint(new Vector3(0,9,6));
        public Vector3 MouthForward => performance!=null?performance.MouthForward:transform.forward;
        public Vector3 Heart => afterImpact&&impactCached?impactPoint:chest!=null?chest.position:transform.TransformPoint(new Vector3(0,8,11));
        public int FractureColumnCount => brokenColumns==null?0:brokenColumns.Length;

        void Update()
        {
            if(!Application.isPlaying&&previewInEditor)Evaluate(previewTime,true);
        }
        public void Initialize()
        {
            if(initialized)return;
            initialized=true;block=new MaterialPropertyBlock();
            surfaces=GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("CH_")).ToArray();
            var all=GetComponentsInChildren<Transform>(true);
            brokenColumns=all.Where(t=>t.name.StartsWith("CH_FractureColumn_")).ToArray();
            revealRibs=all.Where(t=>t.name.StartsWith("CH_RevealRib_")).ToArray();
            columnRest=brokenColumns.Select(t=>t.localRotation).ToArray();
            columnPositions=brokenColumns.Select(t=>t.localPosition).ToArray();
            ribRest=revealRibs.Select(t=>t.localRotation).ToArray();
            well=all.FirstOrDefault(t=>t.name=="CH_FrequencyWell_Surface");
            breach=new DragonBreach(transform,surfaces.FirstOrDefault()?.sharedMaterial);
            if(dragonPrefab!=null)
            {
                var instance=Instantiate(dragonPrefab,transform);instance.name="Current CyberDragon | chamber performance";
                instance.hideFlags=Application.isPlaying?HideFlags.None:HideFlags.DontSave;
                dragon=instance.transform;instance.SetActive(false);dragon.localScale=Vector3.one*dragonScale;
                dragon.localRotation=Quaternion.Euler(0,180,0);
                var bones=instance.GetComponentsInChildren<Transform>(true);
                head=bones.FirstOrDefault(t=>t.name=="head");chest=bones.FirstOrDefault(t=>t.name=="chest");
                animator=instance.GetComponent<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                animator.enabled=false;
                poseNodes=bones.Where(b=>b!=dragon).ToArray();restPositions=poseNodes.Select(b=>b.localPosition).ToArray();
                restRotations=poseNodes.Select(b=>b.localRotation).ToArray();restScales=poseNodes.Select(b=>b.localScale).ToArray();
                performance=new DragonPerformance(dragon,unfoldClip,flyClip?flyClip:glideClip,roarClip);
                dragonRenderers=instance.GetComponentsInChildren<Renderer>(true);
                dragonOriginals=dragonRenderers.Select(r=>r.sharedMaterials).ToArray();
                dragonInstances=dragonOriginals.Select(ms=>ms.Select(m=>new Material(m){hideFlags=HideFlags.DontSave}).ToArray()).ToArray();
                baseColours=dragonOriginals.Select(ms=>ms.Select(m=>m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white).ToArray()).ToArray();
                emissionColours=dragonOriginals.Select(ms=>ms.Select(m=>m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor"):Color.black).ToArray()).ToArray();
                var finishShader=Shader.Find("LordLyrical/Resonant Actor Surface");
                if(finishShader)foreach(var ms in dragonInstances)foreach(var m in ms){m.shader=finishShader;m.SetColor("_EdgeColor",new Color(.05f,.55f,2));}
                for(int i=0;i<dragonRenderers.Length;i++)dragonRenderers[i].sharedMaterials=dragonInstances[i];
            }
        }
        static float Smooth(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        public void Evaluate(double seconds,bool showing=true,Vector3? breathTarget=null)
        {
            Initialize();float t=(float)seconds;
            float wall=t>=59&&t<61?1:0;
            float art=Smooth(96,99,t);
            float glow=showing?Mathf.Lerp(.19f,1.8f,art):.035f;
            glow=Mathf.Max(glow,wall*2.4f);
            if(t>=83&&t<87)glow=Mathf.Max(glow,1.35f);
            if(t>=111)glow*=Mathf.Lerp(1,.25f,Smooth(111,118,t));
            float response=Battle!=null?Mathf.Exp(-Mathf.Max(0,t-Battle.lastSuccess)*3)*.25f+Battle.harmony*.18f:0;glow+=response;
            float cracks=t>=63?(1-art):t>=26&&t<52?1:t>=52&&t<55?1-Smooth(52,55,t):0;
            if(driveMaterialsFromShow)foreach(var r in surfaces)
            {
                r.GetPropertyBlock(block);block.SetFloat("_ShowClock",t);block.SetFloat("_GlyphIntensity",(glow+DragonRevealScore.Glyph(t,r.transform.localPosition.z)*1.3f+Magic33Score.SurfaceWave(t,r.bounds.center.y)*.65f)*Magic33Score.Quiet(t)*glyphIntensityMultiplier*(1+MusicReactiveSystem.Current.bass*.22f+MusicReactiveSystem.Current.pulse*.16f+Shader.GetGlobalFloat("_Crescendo38")*.65f));
                block.SetFloat("_PulseSpeed",glyphPulseSpeed);block.SetColor("_GlyphColor",glyphColour);
                block.SetFloat("_GlyphReveal",t>=59&&t<61?Smooth(59,59.65f,t):1);
                block.SetFloat("_ArtBloom",art);block.SetFloat("_Cracks",cracks);r.SetPropertyBlock(block);
            }
            else if(materialsWereDriven)foreach(var r in surfaces)r.SetPropertyBlock(null);
            materialsWereDriven=driveMaterialsFromShow;
            float fracture=Smooth(DragonRevealScore.Wings,DragonRevealScore.Wings+.45f,t)*(1-Smooth(98,101,t));
            for(int i=0;i<brokenColumns.Length;i++)
            {
                brokenColumns[i].localRotation=columnRest[i]*Quaternion.Euler((fracture+StoryScore40.Reveal(t)*.12f)*(i%2==0?3:-3),0,(fracture+StoryScore40.Reveal(t)*.12f)*(i%2==0?5:-5));
                brokenColumns[i].localPosition=columnPositions[i]+Vector3.down*(fracture*.09f);
            }
            float opening=Smooth(63,68,t);
            for(int i=0;i<revealRibs.Length;i++)
            {
                float side=revealRibs[i].name.Contains("_L_")?-1:1;
                revealRibs[i].localRotation=ribRest[i]*Quaternion.Euler(0,0,side*DragonRevealScore.Rib(t,revealRibs[i].localPosition.z)*27);
                revealRibs[i].gameObject.SetActive(t<100);
            }
            if(well!=null)well.localScale=Vector3.one*(1+Mathf.Sin(t*.8f)*.012f);
            breach.Render(t,showing);
            if(dragon==null)return;
            afterImpact=t>=98;
            if(afterImpact&&!impactCached){
                performance.SampleShow(98,dragonPosition,dragonScale,breathTarget,0);
                impactPoint=chest.position;impactCached=true;
            }
            bool visible=showDragon&&showing&&t>=63&&t<101;dragon.gameObject.SetActive(visible);
            if(!visible)return;
            float dissolve=Mathf.Max(1-DragonRevealScore.Presence(t),Smooth(98,101,t));float impact=Smooth(98,98.12f,t)*(1-Smooth(98.12f,98.6f,t));
            performance.SampleShow(t,dragonPosition,dragonScale,breathTarget,Battle?.rescueCharge??0,Battle?.dragonShots,HeroDuelScore.PlayerReaction(t,Battle));
            float light=DragonRevealScore.Light(t);
            float hitLight=HeroDuelScore.FollowThrough(t)*.16f+HeroDuelScore.PlayerReaction(t,Battle)*.22f;
            for(int i=0;i<dragonInstances.Length;i++)for(int j=0;j<dragonInstances[i].Length;j++)
            {
                var m=dragonInstances[i][j];m.SetFloat("_Magic42",StageMagic42.DragonPower(t)*(.4f+.6f*MusicReactiveSystem.Current.bass));
                // Clip the body behind the broken wall: snout/neck emerge first.
                m.SetVector("_RevealPlane",new Vector4(0,0,-1,transform.TransformPoint(new Vector3(0,0,26.75f+DragonRevealScore.S(64.35f,65.25f,t)*30)).z));
                m.SetFloat("_RevealClip",t<65.25f?1:0);
                m.SetVector("_RevealSphere",new Vector4(head.position.x,head.position.y,head.position.z,2.25f+DragonRevealScore.S(64.35f,65.25f,t)*32));
                if(m.HasProperty("_Dissolve"))m.SetFloat("_Dissolve",dissolve);
                if(m.HasProperty("_Fracture"))m.SetFloat("_Fracture",Mathf.Max(Smooth(90,91,t),Smooth(98,98.3f,t)));
                if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",baseColours[i][j]*light);
                if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",emissionColours[i][j]*DragonRevealScore.EyeLight(t)+new Color(.12f,.72f,1.2f)*(impact*.85f+hitLight));
            }
        }
        // The chamber is hidden during the portal. Keep its sole runtime dragon owned
        // until destruction, so visibility changes cannot create another instance.
        void OnDisable(){if(!Application.isPlaying)Release();else if(dragon!=null)dragon.gameObject.SetActive(false);}
        void OnDestroy(){Release();}
        void Release()
        {
            if(dragon!=null){if(Application.isPlaying)Destroy(dragon.gameObject);else DestroyImmediate(dragon.gameObject);}
            if(dragonInstances!=null)foreach(var ms in dragonInstances)foreach(var m in ms)if(m!=null){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}
            breach?.Dispose();breach=null;
            initialized=false;impactCached=false;afterImpact=false;dragon=null;dragonInstances=null;
        }
    }
}
