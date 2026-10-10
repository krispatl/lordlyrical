Shader "MRGraffiti/Designed metal" {
 Properties {_BaseColor("Color",Color)=(.15,.2,.25,1) _EmissionColor("Selected",Color)=(0,0,0,1) _Metallic("Metal",Range(0,1))=.5}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline"}
 Pass {Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _BaseColor,_EmissionColor;float _Metallic;
 struct A {float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);return o;}
 half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float3 n=normalize(i.n),v=normalize(GetCameraPositionWS()-i.world);float light=.52+.48*saturate(dot(n,normalize(float3(-.4,.8,-.6))));float stripe=pow(saturate(.5+.5*sin(reflect(-v,n).y*8)),18);return half4(_BaseColor.rgb*light+_Metallic*(stripe*.5+pow(1-abs(dot(n,v)),3)*.2)+_EmissionColor.rgb,1);}
 ENDHLSL
 }}
}
