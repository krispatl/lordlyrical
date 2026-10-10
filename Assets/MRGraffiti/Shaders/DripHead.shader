Shader "MRGraffiti/Wet droplet tips" {
 SubShader {Tags {"Queue"="Transparent+2" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass {ZWrite Off Cull Off Offset -3,-3 Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 p:POSITION;float4 c:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float4 c:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.c=i.c;o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=i.uv*2-1;p.x/=lerp(.48,1,smoothstep(-1,.35,p.y));float r=length(p);float a=1-smoothstep(.72,1,r);float shine=pow(saturate(1-length((p-float2(-.25,.1))*3)),3);return half4(i.c.rgb*(.85+.15*saturate(1-r))+shine*.5,a*i.c.a);}
 ENDHLSL
 }}
}
