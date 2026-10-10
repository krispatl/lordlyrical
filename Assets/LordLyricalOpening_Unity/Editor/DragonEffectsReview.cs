using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LordLyrical;

public static class DragonEffectsReview
{
    const string Out=MotionPolishValidation.Out+"/Production/DragonEffects";
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Lord Lyrical/Production/Validate Dragon Effects")]
    public static void Validate()
    {
        Directory.CreateDirectory(Out);
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var report=new StringBuilder("CANDIDATE 17 / DRAGON EFFECTS COHERENCE\n");
        try{
            var cfg=AssetDatabase.LoadAssetAtPath<ExperienceConfiguration>("Assets/LordLyrical/ScriptableObjects/Experience.asset");
            var session=new GameObject("Effects review session").AddComponent<NetworkSessionController>();
            var world=new GameObject("Effects review world").AddComponent<ResonantWorld>();world.Initialize(cfg,session);
            var chamber=world.GetComponentInChildren<ChamberPresentation>();
            var cinema=world.GetComponentInChildren<CinematicParticleDirector>();
            var art=world.GetComponentInChildren<ShowArtPresentation>();
            var ward=cinema.GetComponentsInChildren<MeshRenderer>(true).Single(x=>x.name=="Healer | refractive lattice membrane");
            var ribbons=art.GetComponentsInChildren<LineRenderer>(true).Where(x=>x.name.StartsWith("Pooled resonance ribbon")).ToArray();
            var block=new MaterialPropertyBlock();float maxSurfaceError=0,maxAxisError=0,maxOldAxisError=0,maxGainStep=0,maxGainError=0;
            float previousGain=0;int samples=0,contactSamples=0,beamSamples=0,peakParticles=0;
            Vector4 savedImpact=Vector4.zero;ulong savedParticles=0;
            for(int i=0;i<=13*72;i++){
                float t=68.75f+i/72f;world.Render(ShowFrame.At(t),true);samples++;
                Need(cinema.AllFinite()&&cinema.DroppedParticles==0,"Particle failure at "+t);peakParticles=Math.Max(peakParticles,cinema.ActiveParticles);
                Vector3 target=DragonBreath.Target(t,ConductorRoute.Position(t));float gain=0;
                foreach(var line in ribbons){
                    if(!line.enabled)continue;line.GetPropertyBlock(block);var color=block.GetColor("_Color");
                    if(Mathf.Abs(color.r-.35f)>.00001f||Mathf.Abs(color.g-.65f)>.00001f||Mathf.Abs(color.b-1)>.00001f)continue;
                    if(Vector3.Distance(line.GetPosition(0),chamber.Mouth)>.0001f)continue;
                    Need(Vector3.Distance(line.GetPosition(line.positionCount-1),target)<.0001f,"Core endpoint is not the shared target");
                    gain=block.GetFloat("_Gain");beamSamples++;
                }
                maxGainError=Mathf.Max(maxGainError,Mathf.Abs(gain-DragonBreath.Strength(t)));
                if(i>0)maxGainStep=Mathf.Max(maxGainStep,Mathf.Abs(gain-previousGain));previousGain=gain;
                if(ward.enabled&&t>=70){
                    ward.GetPropertyBlock(block);var impact=block.GetVector("_Impact");
                    Need(float.IsFinite(impact.x)&&float.IsFinite(impact.w),"Invalid impact");
                    if(impact.w>.0001f){
                        contactSamples++;var hit=new Vector3(impact.x,impact.y,impact.z);var local=ward.transform.InverseTransformPoint(hit);
                        maxSurfaceError=Mathf.Max(maxSurfaceError,Mathf.Abs(local.magnitude-1));
                        Need(local.y>=-.0001f,"Contact below hemisphere");
                        var axis=target-chamber.Mouth;float u=Vector3.Dot(hit-chamber.Mouth,axis)/axis.sqrMagnitude;
                        Need(u>=0&&u<=1,"Contact outside beam segment");
                        maxAxisError=Mathf.Max(maxAxisError,Vector3.Distance(hit,Vector3.Lerp(chamber.Mouth,target,u)));
                        var old=new Vector3(Mathf.Sin((t-74)*.75f)*5,2,-2);float oldU=Mathf.Clamp01(Vector3.Dot(old-chamber.Mouth,axis)/axis.sqrMagnitude);
                        maxOldAxisError=Mathf.Max(maxOldAxisError,Vector3.Distance(old,Vector3.Lerp(chamber.Mouth,target,oldU)));
                        Need(impact.w<=DragonBreath.Strength(t)+.0001f,"Impact outlives breath envelope");
                    }
                    if(t>=81)Need(impact.w==0,"Stale shield impact after breath");
                }
            }
            Need(beamSamples>500&&contactSamples>500,"Review missed live breath");
            Need(maxGainError<.00011f&&maxGainStep<.105f,"Core envelope mismatch or discontinuity");
            Need(maxSurfaceError<.00001f&&maxAxisError<.0001f,"Impact not on shield/beam intersection");
            foreach(var sample in new[]{new Vector2(69,0),new Vector2(69.1f,.5f),new Vector2(69.2f,1),new Vector2(80.7f,.5f),new Vector2(81,0)})
                Need(Mathf.Abs(DragonBreath.Strength(sample.x)-sample.y)<.0001f,"Attack/release timing moved");
            foreach(float t in new[]{69f,69.05f,69.1f,69.15f,69.2f}){
                world.Render(ShowFrame.At(t),true);
                var flares=cinema.GetComponentsInChildren<ParticleSystem>(true).Single(x=>x.name=="04 | impact corona");
                var particles=new ParticleSystem.Particle[80];int n=flares.GetParticles(particles);float actualCharge=0;
                for(int j=0;j<n;j++)if(particles[j].randomSeed==6901&&particles[j].GetCurrentSize(flares)>2.6f)actualCharge=particles[j].startColor.a/255f;
                Need(Mathf.Abs(actualCharge+DragonBreath.Strength(t)-1)<.004f,"Charge flare does not hand off continuously to breath at "+t);
            }
            Need(DragonBreath.CounterStrength(76)==0&&Mathf.Abs(DragonBreath.CounterStrength(76.15f)-.5f)<.0001f&&DragonBreath.CounterStrength(76.3f)==1,"Counterbeam attack moved");
            Vector3 hitPoint;
            Need(!DragonBreath.TryWardContact(new Vector3(0,2,8),new Vector3(0,2,7),Vector3.zero,6.2f,out hitPoint),"Short segment falsely hits");
            Need(!DragonBreath.TryWardContact(new Vector3(8,2,0),new Vector3(8,2,5),Vector3.zero,6.2f,out hitPoint),"Miss falsely hits");
            Need(!DragonBreath.TryWardContact(new Vector3(0,-2,8),new Vector3(0,-2,0),Vector3.zero,6.2f,out hitPoint),"Lower hemisphere falsely hits");
            Need(DragonBreath.TryWardContact(new Vector3(0,2,0),new Vector3(0,2,9),Vector3.zero,6.2f,out hitPoint)&&Mathf.Abs(hitPoint.magnitude-6.2f)<.00001f,"Inside-to-outside ray fails");
            world.Render(ShowFrame.At(76),true);ward.GetPropertyBlock(block);savedImpact=block.GetVector("_Impact");savedParticles=cinema.Fingerprint();
            world.Render(ShowFrame.At(94),true);world.ResetEffects();world.Render(ShowFrame.At(76),true);ward.GetPropertyBlock(block);
            Need((block.GetVector("_Impact")-savedImpact).sqrMagnitude<1e-10f&&cinema.Fingerprint()==savedParticles,"Seek/reset differs");
            report.AppendLine($"{samples} full-world samples at 72 Hz: {beamSamples} live core samples; {contactSamples} shield contact samples; peak particles {peakParticles}; zero invalid particles or dropped particles.");
            report.AppendLine($"Core gain error={maxGainError:F7}; maximum one-frame gain step={maxGainStep:F6}. Attack 69-69.2s, release 80.4-81s unchanged from established particles; JERU counterbeam ramps 76-76.3s.");
            report.AppendLine($"Shield local-radius error={maxSurfaceError:F8}; beam-axis error={maxAxisError:F7}m. Prior unrelated impact path was up to {maxOldAxisError:F4}m off the beam in this range.");
            report.AppendLine("PASS native charge-flare particle alpha hands off continuously to the core over 69-69.2s; no stale impact after 81s; hemisphere/short/miss/inside ray cases; deterministic seek/reset. All measurements are editor results, not a headset FPS or comfort check.");
            var camera=new GameObject("Effects review camera").AddComponent<Camera>();camera.cullingMask=1<<28;camera.fieldOfView=82;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.backgroundColor=new Color(.006f,.009f,.016f);camera.clearFlags=CameraClearFlags.SolidColor;CinematicLook.Configure(camera);
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.color=new Color(.63f,.77f,1);light.transform.rotation=Quaternion.Euler(35,-30,0);light.cullingMask=1<<28;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.12f,.16f);
            foreach(float t in new[]{69f,69.1f,69.2f,74.3f,76f,80.4f,80.7f,80.95f,81f}){
                world.Render(ShowFrame.At(t),true);camera.transform.position=new Vector3(-5,2.8f,-5);camera.transform.LookAt(new Vector3(0,4,4));
                foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=28;
                ParticipantAvatarBuilder.Render(camera,Out+"/Effects_"+t.ToString("000.00",System.Globalization.CultureInfo.InvariantCulture)+".png",1200,900,1);
            }
            File.WriteAllText(Out+"/Validation.txt",report.ToString());Debug.Log("DRAGON_EFFECTS_VALIDATION_PASS\n"+report);
        }catch(Exception error){File.WriteAllText(Out+"/Validation_Failed.txt",report+"\n"+error);throw;}
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
