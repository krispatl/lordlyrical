Shader "LordLyrical/Cinematic Wavefront"
{
 Properties { _Noise("Flow",2D)="gray"{} [HDR]_Color("Frequency",Color)=(.1,.6,1,1) _Energy("Radiance",Float)=2 _Age("Progress",Range(0,1))=.3 _Style("Wave 0 / seal 1",Float)=0 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent"}
  Pass {
   Blend One One ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   TEXTURE2D(_Noise);SAMPLER(sampler_Noise);
   CBUFFER_START(UnityPerMaterial)
   float4 _Color;float _Energy,_Age,_Style;
   CBUFFER_END
   float _ShowTime;
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float eye:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float3 w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(w);o.eye=-TransformWorldToView(w).z;o.uv=a.uv;return o;}
   half4 frag(V i):SV_Target{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=i.uv*2-1;float r=length(p);float a=atan2(p.y,p.x);
    float n=SAMPLE_TEXTURE2D(_Noise,sampler_Noise,p*.67+float2(_ShowTime*.035,-_ShowTime*.043)).r;
    float rim=exp(-pow(abs((r-.76+(n-.5)*.07)*55),2));
    float wake=exp(-pow(abs((r-.65)*13),2))*(.20+.80*pow(saturate(n),2));
    float harmonics=pow(saturate(.5+.5*cos(r*125+a*4+_ShowTime*3)),14)*exp(-pow(abs((r-.63)*8),2))*.22;
    if(_Style>.5){rim+=exp(-pow(abs((r-.45)*90),2))*.5;harmonics+=pow(saturate(.5+.5*sin(a*12+sin(r*22))),28)*smoothstep(.35,.5,r)*smoothstep(.75,.65,r)*.65;}
    float edge=smoothstep(1,.87,r)*smoothstep(0,.06,_Age)*pow(saturate(1-_Age),.8);
    float nearFade=saturate((i.eye-.25)/.35);
    float intensity=(rim+wake+harmonics)*edge*_Energy*_Color.a*nearFade;
    return half4(_Color.rgb*intensity,0);
   }
   ENDHLSL
  }
 }
}

