Shader "LordLyrical/Intact Orc Surface"
{
 Properties {
 _BaseMap("Surface atlas",2D)="white"{} _EmissionMap("Emission pattern",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
 _Metallic("Metallic",Range(0,1))=.62 _Smoothness("Smoothness",Range(0,1))=.5
 [HDR]_EmissionColor("Resonance",Color)=(0,0,0,1) [HDR]_EdgeColor("Released frequency",Color)=(2,.38,.015,1)
 _Dissolve("Dissolve",Range(0,1))=0 _Fracture("Fracture",Range(0,1))=0 _Cull("Cull",Float)=2
 }
 SubShader {
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 HLSLINCLUDE
 #define LL_INTACT_ORC 1
 #include "Assets/LordLyricalFinishing_Unity/Shaders/ResonantActorShared.hlsl"
 ENDHLSL
 Pass {
 Name "ForwardLit" Tags{"LightMode"="UniversalForward"} Cull [_Cull]
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 half4 frag(V v):SV_Target{
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);float g=cut(v);
 SurfaceData s=(SurfaceData)0;s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb;s.metallic=_Metallic;s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;
 // These controls are uniform per material. Most combat surfaces have neither
 // fracture nor glow, so skip their noise/trigonometry and emission-map fetch.
 float edge=_Dissolve>.001?1-smoothstep(0,.07,g-_Dissolve):0;
 if(_Fracture>0){float cell=abs(sin(v.local.x*19+sin(v.local.y*13))*sin(v.local.z*17+v.local.y*9));edge+=_Fracture*(1-smoothstep(.015,.06,cell));}
 float3 emissionPattern=0;
 if(any(_EmissionColor.rgb!=0))emissionPattern=_EmissionColor.rgb*SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,v.uv).rgb;
 s.emission=emissionPattern+_EdgeColor.rgb*edge;
 InputData d=(InputData)0;d.positionWS=v.positionWS;d.normalWS=normalize(v.normalWS);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(v.positionWS);d.shadowCoord=TransformWorldToShadowCoord(v.positionWS);d.bakedGI=SampleSH(d.normalWS);d.vertexLighting=v.vertexLight;d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(v.positionCS);d.shadowMask=1;
 half4 color=UniversalFragmentPBR(d,s);color.rgb=MixFog(color.rgb,v.fog);return color;}
 ENDHLSL
 }
 Pass {
 Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask R Cull [_Cull]
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment depth
 #pragma multi_compile_instancing
 half4 depth(V v):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);cut(v);return 0;}
 ENDHLSL
 }
 Pass {
 Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
 HLSLPROGRAM
 #pragma vertex shadow
 #pragma fragment depth
 #pragma multi_compile_instancing
 #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
 float3 _LightDirection,_LightPosition;
 V shadow(A v){V o=vert(v);float3 direction=_LightDirection;
 #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
 direction=normalize(_LightPosition-o.positionWS);
 #endif
 o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction));
 #if UNITY_REVERSED_Z
 o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
 #else
 o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
 #endif
 return o;}
 half4 depth(V v):SV_Target{cut(v);return 0;}
 ENDHLSL
 }
 }
}
