using UnityEngine;
using UnityEngine.Rendering;
namespace LordLyrical
{
    public sealed class CombatImpactVFX : MonoBehaviour
    {
        ParticleSystem sparks,dust,flash,marks,rings;Material markMaterial; readonly AudioSource[] audioPool=new AudioSource[6];
        AudioClip metal,stone,shatter;int audioIndex,seed;float nextSound,windowAt;int windowCount;
        public int ActiveParticles=>(sparks?sparks.particleCount:0)+(dust?dust.particleCount:0)+(flash?flash.particleCount:0)+(marks?marks.particleCount:0)+(rings?rings.particleCount:0);
        public const int ParticleBudget=992;
        public int FlashParticles=>flash?flash.particleCount:0;
        public int RingParticles=>rings?rings.particleCount:0;
        public void Initialize(CinematicParticleDirector source)
        {
            Material spark=source?source.sparksMaterial:Resources.Load<Material>("Concert/BeatAccents");
            Material smoke=source?source.plumeMaterial:spark;
            sparks=Create("Contact sparks",spark,640);dust=Create("Contact dust",smoke,160);flash=Create("Contact flare",source?source.flareMaterial:spark,64);
            markMaterial=new Material(Resources.Load<Shader>("Cinema26/ContactMark"));marks=Create("Contact surface marks",markMaterial,64);marks.GetComponent<ParticleSystemRenderer>().alignment=ParticleSystemRenderSpace.World;
            rings=Create("Contact expanding harmonics",markMaterial,64);rings.GetComponent<ParticleSystemRenderer>().alignment=ParticleSystemRenderSpace.World;
            var grow=rings.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.18f,1,1.7f));
            metal=Resources.Load<AudioClip>("LordLyricalAccents/TuningForkImpact");
            stone=Resources.Load<AudioClip>("LordLyricalAccents/RitualFootfall");
            shatter=Resources.Load<AudioClip>("LordLyricalAccents/GlassResonance");
            for(int i=0;i<audioPool.Length;i++){var a=new GameObject("Impact sound "+i).AddComponent<AudioSource>();a.transform.SetParent(transform,false);a.playOnAwake=false;a.spatialBlend=1;a.minDistance=1.5f;a.maxDistance=22;a.rolloffMode=AudioRolloffMode.Linear;a.priority=160;audioPool[i]=a;}
        }
        ParticleSystem Create(string label,Material material,int count)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=ps.main;m.playOnAwake=false;m.maxParticles=count;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startSpeed=0;m.startLifetime=.4f;m.loop=true;
            var e=ps.emission;e.enabled=false;var shape=ps.shape;shape.enabled=false;var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.08f;r.sortMode=label.Contains("dust")?ParticleSystemSortMode.Distance:ParticleSystemSortMode.None;
            if(label.Contains("dust")){var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=8;sheet.numTilesY=8;sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));}
            else if(label.Contains("sparks")){r.renderMode=ParticleSystemRenderMode.Stretch;r.velocityScale=.035f;r.lengthScale=2;m.gravityModifier=.45f;}
            r.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});
            var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.5f),new GradientAlphaKey(0,1)});fade.color=gradient;
            ps.Play();return ps;
        }
        float Noise(){seed=(seed*1664525+1013904223);return (uint)seed/(float)uint.MaxValue;}
        public void Hit(Vector3 point,Vector3 normal,Color colour,bool armour,bool lethal=false,bool sustained=false)
        {
            if(!sparks||!ManualResonanceBeams.Finite(point)||!ManualResonanceBeams.Finite(normal))return;
            if(Time.unscaledTime-windowAt>.1f){windowAt=Time.unscaledTime;windowCount=0;}
            if(!lethal&&windowCount++>=8)return;
            Vector3 n=normal.sqrMagnitude>.1f?normal.normalized:Vector3.up;Vector3 u=Vector3.Cross(n,Vector3.up).normalized;if(u.sqrMagnitude<.1f)u=Vector3.right;Vector3 v=Vector3.Cross(n,u);
            int count=lethal?48:sustained?5:armour?20:12;
            for(int i=0;i<count;i++){
                float angle=Noise()*Mathf.PI*2;var velocity=n*(.4f+Noise()*1.6f)+(u*Mathf.Cos(angle)+v*Mathf.Sin(angle))*(.5f+Noise()*(lethal?4:2));
                var ep=new ParticleSystem.EmitParams{position=point+n*.025f,velocity=velocity,startLifetime=.12f+Noise()*(lethal?.7f:sustained?.20f:.38f),startSize=(.01f+Noise()*.026f)*(sustained?.7f:1),startColor=Color.Lerp(colour,new Color(1,.78f,.35f),Noise()*.6f),randomSeed=(uint)(i+seed)};
                sparks.Emit(ep,1);
            }
            if(!sustained)marks.Emit(new ParticleSystem.EmitParams{position=point+n*.012f,rotation3D=Quaternion.LookRotation(n).eulerAngles,startLifetime=armour?.25f:lethal?1.2f:.7f,startSize=lethal?.6f:.20f,startColor=colour},1);
            if(!sustained)dust.Emit(new ParticleSystem.EmitParams{position=point+n*.07f,velocity=n*.35f+Vector3.up*.2f,startLifetime=lethal?1.2f:.55f,startSize=lethal?.65f:.18f,startColor=new Color(.24f,.30f,.4f,lethal?.26f:.15f)},lethal?7:2);
            if(!sustained)rings.Emit(new ParticleSystem.EmitParams{position=point+n*.016f,rotation3D=Quaternion.LookRotation(n).eulerAngles,startLifetime=lethal?.65f:.35f,startSize=lethal?.75f:.32f,startColor=colour},1);
            if(!sustained)flash.Emit(new ParticleSystem.EmitParams{position=point+n*.03f,startLifetime=.09f,startSize=lethal?.5f:.18f,startColor=Color.Lerp(colour,Color.white,.6f)},1);
            if(!sustained&&Application.isPlaying&&(lethal||Time.unscaledTime>=nextSound)){
                nextSound=Time.unscaledTime+.10f;var a=audioPool[audioIndex++%audioPool.Length];a.transform.position=point;a.clip=lethal?shatter:armour?metal:stone;a.volume=lethal?.13f:.035f;a.pitch=.9f+Noise()*.2f;if(a.clip)a.Play();
            }
        }
        public void SimulatePreview(float seconds){sparks.Simulate(seconds,false,false);dust.Simulate(seconds,false,false);flash.Simulate(seconds,false,false);marks.Simulate(seconds,false,false);rings.Simulate(seconds,false,false);}
        public void Clear(){if(sparks)sparks.Clear();if(dust)dust.Clear();if(flash)flash.Clear();if(marks)marks.Clear();if(rings)rings.Clear();seed=0;windowCount=0;windowAt=nextSound=0;foreach(var a in audioPool)if(a)a.Stop();}
        void OnDisable()=>Clear();
        void OnDestroy(){if(markMaterial){if(Application.isPlaying)Destroy(markMaterial);else DestroyImmediate(markMaterial);}}
    }
}
