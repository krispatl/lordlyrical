Shader "LordLyrical/Cinematic Particles"
{
 Properties {
  _BaseMap("Particle / 64-frame volume",2D)="white"{} [HDR]_Tint("Tint",Color)=(1,1,1,1)
  _Monochrome("Neutralize texture hue",Range(0,1))=0
  _Energy("Radiance",Float)=2 _Flipbook("Interpolate frames",Float)=0
  _SoftDistance("Intersection fade metres",Float)=.45 _NearFade("Viewer clearance metres",Float)=.35
  [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Destination blend",Float)=1
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"}
  Pass {
   Blend One [_DstBlend] ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint;float _Energy,_Flipbook,_SoftDistance,_NearFade,_DstBlend,_Monochrome;
   CBUFFER_END
   struct A {float4 p:POSITION;half4 color:COLOR;float4 uv:TEXCOORD0;float blend:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 p:SV_POSITION;half4 color:COLOR;float4 uv:TEXCOORD0;float blend:TEXCOORD1;float eye:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float3 w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(w);o.eye=-TransformWorldToView(w).z;o.uv=a.uv;o.blend=a.blend;o.color=a.color;return o;}
   half4 frag(V i):SV_Target {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv.xy);
    if(_Flipbook>.5)tex=lerp(tex,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv.zw),saturate(i.blend));
    tex.rgb=lerp(tex.rgb,max(tex.r,max(tex.g,tex.b)).xxx,_Monochrome);
    float fade=saturate((i.eye-_NearFade)/.45);
    if(_SoftDistance>.001){float raw=SampleSceneDepth(GetNormalizedScreenSpaceUV(i.p.xy));float scene=LinearEyeDepth(raw,_ZBufferParams);fade*=saturate((scene-i.eye)/_SoftDistance);}
    half alpha=tex.a*i.color.a*_Tint.a*fade;
    return half4(tex.rgb*i.color.rgb*_Tint.rgb*_Energy*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
