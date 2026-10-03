Shader "LordLyrical/Resonance"
{
 Properties { _BaseColor("Stone",Color)=(.025,.032,.047,1) _Color("Frequency",Color)=(.1,.8,1,1) _Glow("Glow",Float)=1 _Frequency("Frequency",Float)=5 _Phase("Phase",Float)=0 _Evolution("Evolution",Float)=0 _Thickness("Edge",Float)=.035 _Amplitude("Amplitude",Float)=1 _Dissolve("Dissolve",Range(0,1))=0 _Cracks("Cracks",Range(0,1))=0 _Environment("Environment tint",Float)=0 }
 SubShader { Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_Color;float _Glow,_Frequency,_Phase,_Evolution,_Thickness,_Amplitude,_Dissolve,_Cracks,_Environment;
 CBUFFER_END
 float _ShowTime;float4 _ParticipantTint;
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;}
 float hash(float3 p){return frac(sin(dot(floor(p*7),float3(12.9898,78.233,39.425)))*43758.5453);}
 half4 frag(V i):SV_Target
 {
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 if(_Dissolve>.0001)clip(hash(i.world)-_Dissolve*.999-.001);
 float3 n=abs(normalize(i.normal));float2 p=n.y>.65?i.world.xz:n.x>.65?i.world.zy:i.world.xy;
 float x=p.x*.5,y=p.y*.5;
 float f=cos(_Frequency*x+_Phase)*cos((_Frequency+2)*y)-cos((_Frequency+2)*x)*cos(_Frequency*y+_Phase);
 float width=max(_Thickness,fwidth(f)*1.2);float nodalLine=1-smoothstep(width,width*2,abs(f));
 float r=length(p);float radial=pow(saturate(.5+.5*cos(r*5-_Phase)),22);
 float grit=.78+.22*hash(i.world*.7);float edge=pow(1-saturate(dot(normalize(i.normal),normalize(GetWorldSpaceViewDir(i.world)))),3);
 float hue=frac(atan2(y,x)/6.28318+r*.055);float3 spectrum=.5+.5*cos(6.28318*(hue+float3(0,.33,.67)));
 float3 color=lerp(_Color.rgb,spectrum,saturate(_Evolution));color=lerp(color,color*_ParticipantTint.rgb,_Environment*.18*(1-_Evolution));
 float crack=step(.988,sin(p.x*1.8+sin(p.y*2.7)*.17))*_Cracks;
 float light=.28+.72*saturate(dot(normalize(i.normal),normalize(float3(-.4,.8,-.5))));
 return half4(_BaseColor.rgb*grit*light+color*((nodalLine*.62+radial*.11)*_Glow*_Amplitude+edge*.13)+crack*float3(.5,.08,.025),1);
 }
 ENDHLSL }
 }
}

