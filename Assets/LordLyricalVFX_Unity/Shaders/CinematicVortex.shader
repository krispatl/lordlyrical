Shader "LordLyrical/Cinematic Vortex"
{
 Properties{_Noise("Flow",2D)="gray"{} _Strength("Envelope",Float)=1}
 SubShader{
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
  Pass{
   Blend One One ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_Noise);SAMPLER(sampler_Noise);
   CBUFFER_START(UnityPerMaterial)
   float _Strength;
   CBUFFER_END
   float _ShowTime;
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float phase=a.uv.x*18.8496+a.p.z*.15-_ShowTime*.18;float warp=1+sin(phase)*.075;a.p.xy*=warp;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
   half4 frag(V i):SV_Target{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 uv=float2(i.uv.x*3+i.uv.y*.8+_ShowTime*.025,i.uv.y*6-_ShowTime*.16);
    float4 a=SAMPLE_TEXTURE2D(_Noise,sampler_Noise,uv);float4 b=SAMPLE_TEXTURE2D(_Noise,sampler_Noise,uv*2.3+float2(a.g,-a.r)*.16);
    float density=saturate((a.r*.7+b.g*.3-.18)*1.6);
    float filaments=pow(saturate(1-abs(a.b+b.r*.18-.49)*19),5)*(.4+.6*b.a);
    float streams=pow(saturate(.5+.5*sin((i.uv.x+i.uv.y*.17)*50.265+a.g*8-_ShowTime*.5)),13)*density;
    float3 color=lerp(float3(.075,.055,.33),float3(.02,.38,.55),smoothstep(.3,.7,a.a));
    float fade=smoothstep(0,.045,i.uv.y)*(1-smoothstep(.88,1,i.uv.y));
    float3 result=color*density*.85+color*filaments*1.9+float3(.15,.44,.66)*streams*.5;
    return half4(result*fade*_Strength,0);
   }
   ENDHLSL
  }
 }
}
