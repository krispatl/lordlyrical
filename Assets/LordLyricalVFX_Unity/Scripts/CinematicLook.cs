using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace LordLyrical
{
    public sealed class CinematicLook : MonoBehaviour
    {
        public VolumeProfile profile;
        VolumeProfile runtimeProfile;readonly HashSet<int> configured=new();
        void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeginCamera;var volume=GetComponent<Volume>();if(!volume)volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=12;if(profile){runtimeProfile=ScriptableObject.CreateInstance<VolumeProfile>();foreach(var c in profile.components)runtimeProfile.components.Add(Instantiate(c));if(runtimeProfile.TryGet<Bloom>(out var bloom)){bloom.threshold.Override(1.25f);bloom.intensity.Override(.24f);}if(runtimeProfile.TryGet<ColorAdjustments>(out var grade))grade.saturation.Override(-3);volume.sharedProfile=runtimeProfile;}}
        void BeginCamera(ScriptableRenderContext context,Camera camera){if(camera.cameraType!=CameraType.Game||!configured.Add(camera.GetInstanceID()))return;Configure(camera);}
        public static void Configure(Camera camera){camera.allowHDR=camera.backgroundColor.a>=.999f;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=camera.backgroundColor.a>=.999f;data.requiresDepthTexture=true;data.requiresColorTexture=false;data.antialiasing=AntialiasingMode.None;}
        void OnDisable(){RenderPipelineManager.beginCameraRendering-=BeginCamera;configured.Clear();if(runtimeProfile){foreach(var c in runtimeProfile.components){if(Application.isPlaying)Destroy(c);else DestroyImmediate(c);}if(Application.isPlaying)Destroy(runtimeProfile);else DestroyImmediate(runtimeProfile);runtimeProfile=null;}}
    }
}
