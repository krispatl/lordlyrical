Shader "LordLyrical/Fist lattice shield" {
 Properties {_Tint("Tint",Color)=(0,1,1,1) _Opacity("Opacity",Float)=1 _Clock("Clock",Float)=0 _ImpactAt("Impact",Float)=-100 _Charge38("Stored energy",Float)=0 _ImpactUV("Contact UV",Vector)=(.5,.5,0,0)}
 SubShader {Tags {"Queue"="Transparent+8" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass {ZWrite Off Cull Off Blend SrcAlpha One ColorMask RGB
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _Tint,_ImpactUV;float _Opacity,_Clock,_ImpactAt,_Charge38;
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);float age=_Clock-_ImpactAt;float flex=age>=0&&age<.6?sin(age*27)*exp(-age*9)*.028*exp(-dot(i.uv-_ImpactUV.xy,i.uv-_ImpactUV.xy)*18):0;i.p.z+=flex;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 p=(i.uv-.5)*2;float r=length(p);clip(1-r);float2 q=p*9;q.x+=floor(q.y)*.5;float2 g=abs(frac(q)-.5);float grid=1-smoothstep(.018,.06,min(g.x,g.y));float edge=exp(-abs(r-.96)*95);float wave=exp(-pow((r-frac(_Clock*.45))*20,2));float age=_Clock-_ImpactAt;float shock=age>=0&&age<.6?exp(-pow((length(p-(_ImpactUV.xy*2-1))-age*2)*14,2))*(1-age/.6):0;float alpha=(.028+grid*.08+edge*.75+wave*.09+shock*.8+_Charge38*(edge*.4+grid*.12))*_Opacity;return half4(_Tint.rgb*1.4,alpha);}
 ENDHLSL
 }}
}
