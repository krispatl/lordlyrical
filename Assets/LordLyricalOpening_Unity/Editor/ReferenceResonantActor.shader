Shader "Hidden/LordLyrical/Reference Resonant Actor"
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
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST,_BaseColor,_EmissionColor,_EdgeColor;
 float _Metallic,_Smoothness,_Dissolve,_Fracture,_Cull;
 CBUFFER_END
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;float3 local:TEXCOORD4;half3 vertexLight:TEXCOORD5;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A v){V o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.local=v.positionOS.xyz;o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.vertexLight=VertexLighting(p.positionWS,o.normalWS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
 float grain(float3 p){float3 q=frac(floor(p*13)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
 float cut(V v){float g=grain(v.local);if(_Dissolve>.0001)clip(g-_Dissolve);return g;}
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
 float cell=abs(sin(v.local.x*19+sin(v.local.y*13))*sin(v.local.z*17+v.local.y*9));
 float edge=(_Dissolve>.001?1-smoothstep(0,.07,g-_Dissolve):0)+_Fracture*(1-smoothstep(.015,.06,cell));
 s.emission=_EmissionColor.rgb*SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,v.uv).rgb+_EdgeColor.rgb*edge;
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
