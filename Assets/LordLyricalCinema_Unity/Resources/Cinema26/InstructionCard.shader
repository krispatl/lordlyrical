Shader "LordLyrical/Spatial Instruction Card" {
Properties{_Progress("Alignment",Range(0,1))=0 _Opacity("Opacity",Range(0,1))=1 _ProgressVisible("Show progress",Float)=1 _Accent("Accent",Color)=(.16,.65,1,1)}
SubShader {
Tags {"Queue"="Transparent+30" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
Pass {
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
CBUFFER_START(UnityPerMaterial)
float _Progress,_Opacity,_ProgressVisible;float4 _Accent;
CBUFFER_END
struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=TransformObjectToHClip(i.vertex.xyz);o.uv=i.uv;return o;}
half4 frag(V i):SV_Target {
UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
float2 q=abs((i.uv-.5)*float2(3.0,1)) - float2(1.46,.46);
float d=length(max(q,0))+min(max(q.x,q.y),0)-.035;
float shape=1-smoothstep(-.004,.004,d),rim=(1-smoothstep(.003,.012,abs(d+.008)))*shape;
float rail=step(.06,i.uv.x)*step(i.uv.x,.94)*(1-smoothstep(.009,.014,abs(i.uv.y-.055)));
rail*=_ProgressVisible;float progress=step(i.uv.x,.06+.88*_Progress)*rail;
float3 c=float3(.017,.026,.04)+rim*float3(.14,.22,.28)+rail*.02+progress*_Accent.rgb;
return half4(c,shape*.80*_Opacity);
}
ENDHLSL
}
}
}