Shader "LordLyrical/Magical Tracked Hand" {
 Properties { _BaseMap("Skin",2D)="white"{} _BaseColor("Skin tint",Color)=(.52,.62,.7,1) _HandPower("Palm energy",Float)=0 _RootTint40("Root Key",Color)=(.14,.55,.85,1) }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass {
 ZWrite On ZTest LEqual Cull Back Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_RootTint40;float _HandPower;
 CBUFFER_END
 float _MagicClock,_MagicEnergy;
 struct A{float4 position:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float rim=pow(1-saturate(dot(normalize(i.normal),GetWorldSpaceNormalizeViewDir(i.world))),2.4);
 float current=pow(saturate(sin(i.uv.y*44+i.uv.x*12-_MagicClock*3.2)),18);
 float shade=.58+.42*saturate(dot(normalize(i.normal),normalize(float3(-.3,1,-.4))));
 float3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*shade;
 float3 magic=_RootTint40.rgb*(rim*(.55+_HandPower*.20)+current*(.06+_HandPower*.18))*_MagicEnergy;
 return half4(base+magic,lerp(1,.80,_MagicEnergy)+rim*.07*_MagicEnergy);
 }
 ENDHLSL
 }
 }
}
