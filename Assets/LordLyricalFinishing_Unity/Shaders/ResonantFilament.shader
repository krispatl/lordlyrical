Shader "LordLyrical/Resonant Filament"
{
 Properties { [HDR]_Color("Frequency",Color)=(.1,.6,1,1) _Gain("Energy",Float)=1 _Mode("Ribbon 0 / surface 1",Float)=0 _CoreWhite("Core white",Float)=.35 _Flow("Turbulent flow",2D)="gray"{} }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha One
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_Flow);SAMPLER(sampler_Flow);
 CBUFFER_START(UnityPerMaterial)
 float4 _Color; float _Gain,_Mode,_CoreWhite;
 CBUFFER_END
 float _ShowTime;
 struct A { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct V { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO };
 V vert(A v){ V o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.world=TransformObjectToWorld(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o; }
 half4 frag(V v):SV_Target {
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
 float2 noiseUV=float2(v.uv.x*3-_ShowTime*.7,v.uv.y*.35+_ShowTime*.08);
 float n=SAMPLE_TEXTURE2D(_Flow,sampler_Flow,noiseUV).r;
 float across=abs(v.uv.y*2-1+(n-.5)*.16);
 float edge=pow(saturate(1-across),2.2);
 float core=exp(-across*across*70);
 float pulse=.65+.35*sin(v.uv.x*50-_ShowTime*12+n*4);
 if(_Mode>.5){edge=1;core=0;pulse=.25+.75*pow(.5+.5*sin(v.world.z*.6-_ShowTime*4),5);}
 float alpha=edge*saturate(_Gain)*_Color.a*v.color.a;
 return half4((_Color.rgb*(1.5+pulse*1.6)+core*_CoreWhite)*v.color.rgb,alpha);
 }
 ENDHLSL
 }
 }
}
