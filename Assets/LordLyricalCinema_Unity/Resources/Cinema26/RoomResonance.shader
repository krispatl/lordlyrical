Shader "LordLyrical/Room Resonance" {
 Properties { _MainTex("Resonance seal",2D)="white"{} _Energy("Energy",Float)=0 _Clock("Song time",Float)=0 _PortalRadius("Aperture occlusion",Float)=0 }
 SubShader {
 Tags {"Queue"="Transparent+10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 ZWrite Off Cull Off Blend SrcAlpha One ColorMask RGB Offset -1,-1
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float _Energy,_Clock,_PortalRadius;
 CBUFFER_END
 struct A{float4 vertex:POSITION;float3 normal:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 vertex:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.vertex.xyz);o.vertex=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);return o;}
 half4 frag(V i):SV_Target {
 UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
 float3 n=abs(normalize(i.normal));float2 p=n.y>.55?i.world.xz:n.x>n.z?i.world.zy:i.world.xy;
 float2 tile=floor(p*.6),cell=frac(p*.6)-.5;
 float chosen=step(.67,frac(sin(dot(tile,float2(127.1,311.7)))*43758.5453));
 float4 seal=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,cell+.5);
 float r=length(i.world-float3(0,0,5));
 float age=_Clock<9?_Clock+.85:_Clock-118.2;
 float wave=exp(-pow((r-age*2.6)*1.8,2));
 float arrived=smoothstep(r-.6,r+.6,age*2.6);
 float2 grid=abs(frac(p*1.68)-.5);float gridLine=1-smoothstep(.005,.018,min(grid.x,grid.y));
 float twinkle=.6+.4*sin(_Clock*2.7+r*3.1);
 float approach=smoothstep(-4.5,-2.2,_Clock)*(1-smoothstep(-1,-.8,_Clock));
 float prewave=exp(-pow((r-(_Clock+4.5)*.85)*2.2,2))*approach*.06;
 float fracture=abs(sin(p.x*3.9+sin(p.y*2.3)*1.8)+sin(p.y*4.7+sin(p.x*1.7)*1.2));
 float crack=1-smoothstep(.015,.060,fracture);
 float reveal=smoothstep(-4.5,-1.6,_Clock)*(1-smoothstep(0,1.4,_Clock));
 float farewell=smoothstep(118.2,119,_Clock)*(1-smoothstep(121.5,124,_Clock));
 float ink=crack*(reveal+farewell)*(.035+.025*twinkle)+prewave+seal.a*(.025+.045*twinkle)*arrived*chosen+gridLine*wave*.15+wave*.045;
 if(_PortalRadius>0){
     float3 eye=GetCameraPositionWS(),dir=i.world-eye;
     float hit=(6.35-eye.z)/(abs(dir.z)<.001?.001:dir.z);
     float radius=length(eye.xy+dir.xy*hit-float2(0,1.7));
     if(hit>0&&hit<1)ink*=smoothstep(_PortalRadius-.06,_PortalRadius+.06,radius);
 }
 float3 colour=lerp(float3(.76,.83,.90),float3(1,.72,.29),seal.r*.25+wave*.42);
 return half4(colour*1.7,saturate(ink)*_Energy);
 }
 ENDHLSL
 }
}
}