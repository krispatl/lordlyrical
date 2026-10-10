Shader "LordLyrical/Sonic Orc Surface"
{
 Properties
 {
  _BaseMap("Basalt atlas",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
  _Metallic("Metallic",Range(0,1))=.62
  _Smoothness("Smoothness",Range(0,1))=.64
  [HDR]_EmissionColor("Resonance",Color)=(0,0,0,1)
  _Dissolve("Sonic disintegration",Range(0,1))=0
  _Cutoff("Cutoff",Range(0,1))=.5
  _Cull("Cull",Float)=2
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Name "ForwardLit"
   Tags{"LightMode"="UniversalForward"}
   Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST,_BaseColor,_EmissionColor;
   float _Metallic,_Smoothness,_Dissolve,_Cutoff,_Cull;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A v)
   {
    V o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;
    o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;
   }
   half4 frag(V v):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
    float3 q=frac(floor(v.positionWS*22)*.1031);q+=dot(q,q.yzx+33.33);float grain=frac((q.x+q.y)*q.z);
    if(_Dissolve>.0001)clip(grain-_Dissolve);
    SurfaceData s=(SurfaceData)0;
    s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb;
    s.metallic=_Metallic;s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;
    s.emission=_EmissionColor.rgb+(_Dissolve>.001?half3(2,.38,.015)*(1-smoothstep(0,.065,grain-_Dissolve)):0);
    InputData d=(InputData)0;d.positionWS=v.positionWS;d.normalWS=normalize(v.normalWS);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(v.positionWS);
    d.shadowCoord=TransformWorldToShadowCoord(v.positionWS);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(v.positionCS);d.shadowMask=1;
    half4 color=UniversalFragmentPBR(d,s);color.rgb=MixFog(color.rgb,v.fog);return color;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
