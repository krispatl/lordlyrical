Shader "LordLyrical/Resonance Contact Mark" {
 SubShader {
 Tags {"Queue"="Transparent+5" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
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
 half4 frag(V i):SV_Target{
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=i.uv*2-1;float r=length(p);
 float ring=1-smoothstep(.025,.11,abs(r-.68));float core=pow(saturate(1-r),3)*.35;
 return half4(i.color.rgb,saturate(ring*.45+core)*i.color.a);
 }
 ENDHLSL
 }
 }
}
