Shader "LordLyrical/ChamberSurfel"
{
 Properties{_Glow("Resonance",Float)=0 _Evolution("Evolution",Float)=0}
 SubShader{Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Off
 Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Glow,_Evolution;
 CBUFFER_END
 float _ShowTime;
 struct A{float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 positionCS:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.color=a.color;o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=i.uv*2-1;clip(1-dot(p,p));float f=cos(i.world.x*2)*cos(i.world.y*4)-cos(i.world.x*4)*cos(i.world.y*2);float pattern=1-smoothstep(.04,.12,abs(f));float3 spectrum=.5+.5*cos(length(i.world.xz)*.7+float3(0,2.09,4.19));float3 frequency=lerp(float3(.1,.55,.7),spectrum,_Evolution);return half4(i.color.rgb*.8+frequency*pattern*_Glow*.7,1);}
 ENDHLSL}
 }
}
