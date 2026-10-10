Shader "LordLyrical/ConcertScoreLight" {
SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
Pass { Blend One One ZWrite Off Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 positionOS:POSITION;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;return o;}
half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return i.color;}
ENDHLSL
}}
}
