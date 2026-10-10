Shader "LordLyrical/Spectral Thread 35" {
 SubShader {
 Tags {"Queue"="Transparent+6" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass {
 ZWrite Off Cull Off Blend SrcAlpha One ColorMask RGB
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 position:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 position:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=TransformObjectToHClip(i.position.xyz);o.color=i.color;o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float edge=abs(i.uv.y*2-1);return half4(i.color.rgb*1.35,i.color.a*(1-smoothstep(.18,1,edge)));}
 ENDHLSL
 }
 }
}
