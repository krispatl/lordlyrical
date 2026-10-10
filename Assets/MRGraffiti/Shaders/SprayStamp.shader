Shader "MRGraffiti/Aerosol deposition" {
 SubShader {Cull Off ZWrite Off ZTest Always Blend One OneMinusSrcAlpha
 HLSLINCLUDE
 #include "UnityCG.cginc"
 float4 _Color,_Spray,_Finish;
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.p=float4(i.p.xy,0,1);
 #if UNITY_UV_STARTS_AT_TOP
 o.p.y=-o.p.y;
 #endif
 o.uv=i.uv;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7))+_Spray.w*17.71)*43758.5453);}
 float coverage(float2 uv){float2 p=(uv-.5)*2;float r=length(p);float core=exp(-r*r*lerp(3,6,_Spray.y));float speck=step(1-_Spray.z*.35,hash(floor(uv*180)));float feather=1-smoothstep(.35,1,r);float irregular=.72+.28*hash(floor(uv*63));return saturate((core*feather*irregular+speck*pow(saturate(1-r),.5)*.22)*_Spray.x);}
 half4 pigment(V i):SV_Target {float a=coverage(i.uv);return half4(_Color.rgb*a,a);}
 float4 property(V i):SV_Target {float a=coverage(i.uv);return float4(_Finish.xyz*a,a);}
 ENDHLSL
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment pigment
 ENDHLSL}
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment property
 ENDHLSL}
 }
}
