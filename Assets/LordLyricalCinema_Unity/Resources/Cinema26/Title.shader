Shader "LordLyrical/Cinema Title" {
Properties { _MainTex("Artwork",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Opacity("Opacity",Float)=1 _Dissolve("Dissolve",Range(0,1))=0 }
SubShader {
 Tags {"Queue"="Transparent+20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;float _Opacity,_Dissolve;
 CBUFFER_END
 struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=TransformObjectToHClip(i.vertex.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);float2 cell=floor(i.uv*float2(180,120));float noise=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);float field=.7*noise+.3*i.uv.y;float mask=_Dissolve<=0?1:1-smoothstep(field-.035,field+.035,_Dissolve*1.08);float edge=_Dissolve>0?mask*(1-smoothstep(.02,.09,field-_Dissolve)):0;return half4(c.rgb*_Color.rgb+half3(.15,.45,.65)*edge,c.a*_Color.a*_Opacity*mask); }
 ENDHLSL
 }
}
}