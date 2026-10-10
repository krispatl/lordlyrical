Shader "MRGraffiti/Wall pigment cyber finish" {
 Properties {_Paint("Pigment",2D)="black"{} _Properties("Finish wetness",2D)="black"{}}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass {Cull Off ZWrite Off Offset -2,-2 Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_Paint);SAMPLER(sampler_Paint);TEXTURE2D(_Properties);SAMPLER(sampler_Properties);
 struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float4 paint=SAMPLE_TEXTURE2D(_Paint,sampler_Paint,i.uv);clip(paint.a-.002);float4 props=SAMPLE_TEXTURE2D(_Properties,sampler_Properties,i.uv);float3 parameters=props.rgb/max(.001,props.a);float wet=exp(-max(0,_Time.y-parameters.z)/9);float3 v=normalize(GetCameraPositionWS()-i.world),n=normalize(i.normal);float facing=abs(dot(n,v));float3 reflectDir=reflect(-v,n);float stripe=pow(saturate(.5+.5*sin(reflectDir.y*9+reflectDir.x*3)),12);float3 base=paint.rgb/max(.001,paint.a);float3 holo=.55+.45*cos(6.283*(float3(0,.33,.67)+facing*1.9+i.world.y*.12));float3 chrome=base*.28+(.12+stripe*.8+pow(1-facing,3)*.45);float3 color=lerp(base,chrome,saturate(parameters.x));color=lerp(color,base*.35+holo*(.35+stripe*.6),saturate(parameters.y));color+=wet*pow(saturate(dot(reflectDir,normalize(float3(.4,.8,.5)))),36)*.22;return half4(color,paint.a);}
 ENDHLSL
 }}
}
