Shader "LordLyrical/OpeningPortal"
{
    Properties { _Color("Rim",Color)=(.1,.6,1,1) _Clock("Clock",Float)=0 _Opacity("Opacity",Float)=1 }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float _Clock; float _Opacity;
            CBUFFER_END
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o; }
            half4 frag(V i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=(i.uv-.5)*2;float r=length(p);clip(1-r);
                float angle=atan2(p.y,p.x), depth=-log(max(.025,r)), flow=depth*10-angle*3+_Clock*3;
                float ribbons=pow(saturate(.5+.5*sin(flow+sin(angle*7-depth*2+_Clock)*.6)),10);
                float stars=pow(saturate(sin(angle*43+depth*91)*sin(angle*19-depth*57)),28);
                float rim=exp(-pow((r-.965)*70,2));
                float3 color=float3(.012,.014,.055)+ribbons*float3(.06,.23,.65)*(.3+r*.6)+stars*float3(.3,.6,1);
                color+=pow(saturate(.5+.5*sin(depth*16+_Clock*2)),22)*float3(.025,.10,.16);
                float edgeDust=.35+.65*pow(saturate(.5+.5*sin(angle*61-depth*9+_Clock*1.8)),3);
                color+=rim*_Color.rgb*1.2*edgeDust;
                return half4(color,smoothstep(1,.985,r)*_Opacity);
            }
            ENDHLSL
        }
    }
}