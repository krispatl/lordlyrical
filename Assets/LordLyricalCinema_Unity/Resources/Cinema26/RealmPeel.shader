Shader "LordLyrical/Realm Peel" {
Properties {
 _BaseMap("Surface",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
 _EmissionMap("Emission",2D)="white"{} _EmissionColor("Emission",Color)=(0,0,0,1)
 _Metallic("Metallic",Float)=.2 _Smoothness("Smoothness",Float)=.45
 _CinemaActor("Founder",Float)=0 _CinemaBounds("Vertical extent",Vector)=(0,2.5,0,0)
 _CinemaDissolve("Release",Range(0,1))=0 _Cull("Cull",Float)=2
}
SubShader {
Tags {"Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
Pass {
Tags {"LightMode"="UniversalForward"} Cull [_Cull]
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST,_BaseColor,_EmissionColor,_CinemaBounds;float _Metallic,_Smoothness,_CinemaDissolve,_Cull,_CinemaActor;
CBUFFER_END
struct A{float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V{float4 vertex:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.vertex.xyz);o.vertex=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
half4 frag(V i):SV_Target {
UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
float grain=hash(floor(i.world*6));
float field=min(.98,length((i.world-float3(0,1.5,2))*float3(1,.75,1))/30*.98)+grain*.02;
if(_CinemaActor>.5)field=saturate((i.world.y-_CinemaBounds.x)/max(.05,_CinemaBounds.y-_CinemaBounds.x))*.82+grain*.18;
float threshold=_CinemaDissolve*1.05-.025;clip(field-threshold);
float edge=(1-smoothstep(0,.012,field-threshold))*smoothstep(0,.08,_CinemaDissolve);
SurfaceData s=(SurfaceData)0; s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
s.metallic=_Metallic;s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;
s.emission=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb+edge*float3(.15,.65,.95);
InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(i.normal);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.vertex);d.shadowMask=1;
return UniversalFragmentPBR(d,s);
}
ENDHLSL
}
}
}