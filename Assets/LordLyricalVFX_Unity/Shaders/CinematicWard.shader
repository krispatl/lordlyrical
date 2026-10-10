Shader "LordLyrical/Cinematic Resonance Shield"
{
 Properties { _Noise("Flow",2D)="gray"{} [HDR]_Color("Frequency",Color)=(.1,1,.4,1) _Strength("Envelope",Float)=1 _Impact("Impact world",Vector)=(0,4,0,0) }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+4" "RenderType"="Transparent"}
  Pass {
   Blend One One ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_Noise);SAMPLER(sampler_Noise);
   CBUFFER_START(UnityPerMaterial)
   float4 _Color,_Impact;float _Strength;
   CBUFFER_END
   float _ShowTime;
   struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;return o;}
   half4 frag(V i):SV_Target {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float3 view=GetWorldSpaceNormalizeViewDir(i.w);float fres=pow(saturate(1-abs(dot(normalize(i.n),view))),3);
    float2 q=i.uv*float2(42,16),grid=float2(1,1.732);
    float2 a=(frac(q/grid)-.5)*grid,b=(frac((q-grid*.5)/grid)-.5)*grid;
    float2 cell=abs(dot(a,a)<dot(b,b)?a:b);float d=max(dot(cell,float2(.5,.866)),cell.x);float edge=pow(saturate(d*2),22);
    float noise=SAMPLE_TEXTURE2D(_Noise,sampler_Noise,i.uv*3+_ShowTime*float2(.008,-.021)).r;
    float dist=length(i.w-_Impact.xyz);float contact=exp(-dist*dist*.65)*_Impact.w;
    float ripple=pow(saturate(.5+.5*cos(dist*12-_ShowTime*9)),10)*exp(-dist*.55)*_Impact.w;
    float radiance=(fres*.075+edge*(.009+fres*.035)+noise*.006+contact*.30+ripple*.12)*_Strength;
    return half4(_Color.rgb*radiance,0);
   }
   ENDHLSL
  }
 }
}

