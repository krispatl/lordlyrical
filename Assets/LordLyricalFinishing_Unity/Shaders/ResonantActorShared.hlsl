// Shared surface declarations keep intact and dissolving Orc geometry identical.
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST,_BaseColor,_EmissionColor,_EdgeColor,_RevealPlane,_RevealSphere;
 float _RevealClip;
 float _Metallic,_Smoothness,_Dissolve,_Fracture,_Cull,_Magic42;
 CBUFFER_END
 float _Song42;
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;float3 local:TEXCOORD4;half3 vertexLight:TEXCOORD5;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A v){V o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.local=v.positionOS.xyz;o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.vertexLight=VertexLighting(p.positionWS,o.normalWS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
 float grain(float3 p){float3 q=frac(floor(p*13)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
 float cut(V v){
 #if defined(LL_INTACT_ORC)
 return 1;
 #else
 if(_RevealClip>.5){clip(dot(float4(v.positionWS,1),_RevealPlane));clip(_RevealSphere.w-distance(v.positionWS,_RevealSphere.xyz));}
 if(_Dissolve<=.0001)return 1;float g=grain(v.local);clip(g-_Dissolve);return g;
 #endif
 }
