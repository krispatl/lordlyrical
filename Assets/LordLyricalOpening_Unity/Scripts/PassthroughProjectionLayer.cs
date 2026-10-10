using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.XR.OpenXR.CompositionLayers;
using UnityEngine.XR.OpenXR.NativeTypes;
#endif
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;

namespace LordLyrical
{
    /// <summary>Creates the source-alpha scene layer after the OpenXR provider starts.</summary>
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class PassthroughProjectionLayer : MonoBehaviour
    {
        CompositionLayer layer;
        public static bool NativeAlphaApplied {get;private set;}
        public static string AlphaProbe {get;private set;}="Pending";
        void Awake() { EnsureRegistered(); }
        void OnEnable() { Application.onBeforeRender+=ApplyNativeAlpha; }
        void OnDisable() { Application.onBeforeRender-=ApplyNativeAlpha;NativeAlphaApplied=false; }
        IEnumerator Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // A small, one-time sample of Unity's eye texture. It contains virtual
            // rendering only; no passthrough-camera image is read or saved.
            yield return new WaitForSecondsRealtime(5);
            yield return new WaitForEndOfFrame();
            var director=FindFirstObjectByType<ExperienceDirector>();
            if(!director||director.Opening==null||!director.Opening.PassthroughActive||director.Opening.RoomOpacity<.99f){AlphaProbe="Skipped: opening no longer transparent";yield break;}
            if(!SystemInfo.supportsAsyncGPUReadback){AlphaProbe="Unavailable: async readback unsupported";Debug.Log("PICO_ALPHA_PROBE "+AlphaProbe);yield break;}
            var displays=new List<XRDisplaySubsystem>();SubsystemManager.GetSubsystems(displays);
            foreach(var display in displays)if(display.running&&display.GetRenderPassCount()>0)
            {
                display.GetRenderPass(0,out var pass);
                var target=display.GetRenderTextureForRenderPass(0);
                if(!target){AlphaProbe="Unavailable: eye texture not exposed; format="+pass.renderTargetDesc.graphicsFormat;Debug.Log("PICO_ALPHA_PROBE "+AlphaProbe);yield break;}
                string format=target.graphicsFormat+" "+target.dimension+" "+target.width+"x"+target.height;
                int x=Mathf.Clamp(target.width/4,0,Mathf.Max(0,target.width-16));
                int y=Mathf.Clamp(target.height*3/4,0,Mathf.Max(0,target.height-16));
                AsyncGPUReadback.Request(target,0,x,16,y,16,0,1,TextureFormat.RGBA32,request=>
                {
                    if(request.hasError){AlphaProbe="Readback failed; format="+format;}
                    else
                    {
                        var pixels=request.GetData<Color32>();int min=255,max=0;long sum=0;
                        foreach(var pixel in pixels){min=Mathf.Min(min,pixel.a);max=Mathf.Max(max,pixel.a);sum+=pixel.a;}
                        AlphaProbe=$"samples={pixels.Length} alphaMin={min} alphaMax={max} alphaMean={(pixels.Length>0?sum/(double)pixels.Length:-1):F3}/255; {format}";
                    }
                    Debug.Log("PICO_ALPHA_PROBE "+AlphaProbe);
                });
                yield break;
            }
            AlphaProbe="Unavailable: XR display not running";
#else
            yield break;
#endif
        }
        void ApplyNativeAlpha()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!OpenXRLayerProvider.isStarted||!layer)return;
            var manager=CompositionLayerManager.Instance;
            if(manager==null||manager.DefaultSceneCompositionLayer!=layer)return;
            // Apply after provider initialization and the manager's Update. Registered
            // bookkeeping alone does not prove that the native default flags survived.
            OpenXRLayerUtility.SetDefaultLayerFlags(XrCompositionLayerFlags.SourceAlpha);
            if(!NativeAlphaApplied)Debug.Log("PICO_NATIVE_SOURCE_ALPHA applied after provider start");
            NativeAlphaApplied=true;
#endif
        }
        void OnApplicationFocus(bool focused) { if(focused) EnsureRegistered(); }
        void OnApplicationPause(bool paused) { if(!paused) EnsureRegistered(); }

        void EnsureRegistered()
        {
            if(!Application.isPlaying) return;
            if(!layer)
            {
                var go=new GameObject("Runtime source-alpha projection");
                go.SetActive(false);
                go.transform.SetParent(transform,false);
                layer=go.AddComponent<CompositionLayer>();
                layer.ChangeLayerDataType(new DefaultLayerData{BlendType=BlendType.Premultiply});
                layer.Order=0;
                go.SetActive(true);
            }
            // With an early OpenXR session, the manager exists before its scene callback.
            // Reporting the active layer creates that callback and discovers the default.
            var manager=CompositionLayerManager.Instance;
            manager.CompositionLayerCreated(layer);
            manager.CompositionLayerEnabled(layer);
            Debug.Log("PICO_PROJECTION_LAYER: registered="+(manager.DefaultSceneCompositionLayer==layer)+
                " provider="+(manager.LayerProvider?.GetType().Name??"waiting")+" sourceAlpha=premultiplied");
        }
    }
}
