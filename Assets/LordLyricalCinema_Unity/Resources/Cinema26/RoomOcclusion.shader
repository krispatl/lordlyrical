Shader "LordLyrical/Measured Room Occlusion" {
 Properties { _PortalRadius("Portal radius",Float)=0 }
 SubShader {
 Tags {"Queue"="Geometry-20" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass {
 ZWrite On ColorMask 0 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _PortalRadius;
 CBUFFER_END
 struct A{float4 position:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);return o;}
 half4 frag(V i):SV_Target{
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 if(_PortalRadius>0){
 float3 eye=GetCameraPositionWS(),dir=i.world-eye;
 float hit=(6.35-eye.z)/(abs(dir.z)<.001?.001:dir.z);
 float radius=length(eye.xy+dir.xy*hit-float2(0,1.7));
 if(hit>0&&hit<1)clip(radius-_PortalRadius);
 }
 return 0;
 }
 ENDHLSL
 }
 }
}
