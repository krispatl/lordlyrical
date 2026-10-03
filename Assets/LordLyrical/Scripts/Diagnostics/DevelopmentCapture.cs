using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace LordLyrical
{
    public class DevelopmentCapture:MonoBehaviour
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        ExperienceDirector director;int index;readonly double[] times={21.5,39.5,66.5,85.5,98.8,117};string folder;Camera captureCamera;RenderTexture target;
        void Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--capture-evidence")<0){enabled=false;return;}
            director=GetComponent<ExperienceDirector>();folder=Path.Combine(Application.dataPath,"../Evidence/Player");Directory.CreateDirectory(folder);
            captureCamera=Camera.main;target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);target.Create();captureCamera.enabled=false;
        }
        void LateUpdate()
        {
            if(director==null||captureCamera==null)return;
            // Render every test frame even when Windows hides the player window.
            RenderPipeline.SubmitRenderRequest(captureCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            if(director.Phase==SessionPhase.Ready){index=0;return;}
            while(index<times.Length&&director.ShowTime-times[index]>.5)index++;
            if(index>=times.Length||director.ShowTime<times[index])return;
            var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,$"show-{times[index]:000.0}.png"),pixels.EncodeToPNG());
            Destroy(pixels);RenderTexture.active=previous;index++;
        }
        void OnDestroy(){if(target!=null){target.Release();Destroy(target);}}
#endif
    }
}
