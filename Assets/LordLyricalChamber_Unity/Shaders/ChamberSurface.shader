Shader "LordLyrical/Chamber Surface"
{
    Properties
    {
        _BaseMap("Stone colour",2D)="white"{}
        _BaseColor("Surface tint",Color)=(.12,.16,.20,1)
        _NormalMap("Surface normal",2D)="bump"{}
        _NormalStrength("Carving depth",Range(0,2))=1
        _Metallic("Metallic",Range(0,1))=.2
        _Smoothness("Smoothness",Range(0,1))=.4
        _GlyphMask("Glyph: R shape, G reveal, B sector",2D)="black"{}
        _GlyphMode("Use carved glyph mask",Float)=0
        _GlyphColor("Glyph colour",Color)=(.01,.45,1,1)
        _GlyphIntensity("Glyph intensity",Range(0,8))=.2
        _GlyphReveal("Glyph reveal",Range(0,1))=1
        _PulseSpeed("Pulse speed",Float)=1
        _ArtBloom("Full spectrum art",Range(0,1))=0
        _Cracks("Floor fractures",Range(0,1))=0
        _SurfaceKind("0 stone, 1 floor, 2 metal, 3 Well",Float)=0
        _TextureAmount("Stone texture amount",Range(0,1))=1
        _ShowClock("Synchronized show seconds",Float)=21
        _EmissionColor("Constant inlay light",Color)=(0,0,0,1)
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline"}
        Cull Off
        Pass
        {
            Name "ChamberForward"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_GlyphMask); SAMPLER(sampler_GlyphMask);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor,_GlyphColor,_EmissionColor;
            float _NormalStrength,_Metallic,_Smoothness,_GlyphMode,_GlyphIntensity,_GlyphReveal;
            float _PulseSpeed,_ArtBloom,_Cracks,_SurfaceKind,_TextureAmount,_ShowClock;
            CBUFFER_END
            float _RealmDissolve,_Discord40,_RealmEntrance41;int _ActiveRootMask40;float4 _RootColours40[10];
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half4 tangent:TEXCOORD2;float2 uv:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes i)
            {
                Varyings o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(i.normalOS);o.tangent=half4(TransformObjectToWorldDir(i.tangentOS.xyz),i.tangentOS.w*GetOddNegativeScale());o.uv=i.uv;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n=normalize(i.normal),t=normalize(i.tangent.xyz),b=cross(n,t)*i.tangent.w;
                float3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv),_NormalStrength);
                n=normalize(t*normalTS.x+b*normalTS.y+n*normalTS.z);
                float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));if(dot(n,view)<0)n=-n;
                float3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw).rgb;
                float grain=dot(tex,float3(.3,.59,.11));
                float3 base=_BaseColor.rgb*lerp(1,grain*1.55,_TextureAmount);
                Light key=GetMainLight(TransformWorldToShadowCoord(i.world));float3 l=normalize(float3(-.35,.8,-.4));
                float diffuse=saturate(dot(n,l))*lerp(.35,1,key.shadowAttenuation);
                float heightLift=saturate(i.world.y/17);
                float3 ambient=lerp(float3(.24,.29,.38),float3(.33,.39,.50),heightLift);
                float3 colour=base*(ambient+diffuse*float3(.92,.83,.75));
                #if defined(_ADDITIONAL_LIGHTS)
                uint additionalCount=GetAdditionalLightsCount();for(uint lightIndex=0;lightIndex<additionalCount;lightIndex++){Light extra=GetAdditionalLight(lightIndex,i.world);colour+=base*extra.color*saturate(dot(n,extra.direction))*extra.distanceAttenuation*.38;}
                #endif
                float spec=pow(saturate(dot(n,normalize(l+view))),lerp(12,120,_Smoothness));
                colour+=spec*lerp(.025,.27,_Metallic)*float3(.43,.64,.87);
                float edge=pow(1-saturate(dot(n,view)),4);colour+=edge*_Metallic*.025;
                float2 glyphUV=i.uv+float2(sin(i.world.y*7+_ShowClock*13),sin(i.world.x*5-_ShowClock*11))*.006*_Discord40;
                float4 mask=SAMPLE_TEXTURE2D(_GlyphMask,sampler_GlyphMask,glyphUV);
                float pulse=.78+.22*sin(_ShowClock*_PulseSpeed*2.6-mask.g*9+i.world.y*.22);
                float reveal=smoothstep(mask.g-.08,mask.g+.02,_GlyphReveal);
                int rootIndex=min(9,(int)(frac(mask.b+i.world.y*.013)*10));float connected=(_ActiveRootMask40&(1<<rootIndex))!=0?1:0;float3 spectrum=_RootColours40[rootIndex].rgb*connected;
                float3 glow=lerp(_GlyphColor.rgb,spectrum,_ArtBloom);glow=lerp(glow,float3(.8,.24,.04),_Discord40*.42);
                colour+=_GlyphMode*mask.r*glow*(_GlyphIntensity*pulse*reveal);
                // Dark normal-mapped incisions remain legible with emission at zero.
                colour*=1-_GlyphMode*mask.r*.2;
                colour+=_EmissionColor.rgb*(.55+.45*pulse);
                if(_SurfaceKind>.5&&_SurfaceKind<1.5)
                {
                    float2 p=i.world.xz;
                    float fissure=abs(sin(p.x*1.37+sin(p.y*2.8)*.12));
                    float fissureLine=1-smoothstep(.012,.04,fissure);
                    colour=lerp(colour,colour*.10,fissureLine*_Cracks);
                    colour+=fissureLine*_Cracks*float3(.1,.48,.8)*.25;
                    float f=cos(p.x*1.9)*cos(p.y*2.7)-cos(p.x*2.7)*cos(p.y*1.9);
                    float art=1-smoothstep(.03,.09,abs(f));
                    int artRoot=min(9,(int)(frac(atan2(p.y-10,p.x)/6.283185+.5)*10));float3 artColour=(_ActiveRootMask40&(1<<artRoot))!=0?_RootColours40[artRoot].rgb:float3(0,0,0);
                    colour+=art*_ArtBloom*artColour*.5*lerp(1,.22,smoothstep(96,96.4,_ShowClock)*(1-smoothstep(97.8,98,_ShowClock)));
                }
                if(_SurfaceKind>2.5)
                {
                    float r=length(i.world.xz-float2(0,10));
                    float wave=pow(saturate(.5+.5*cos(r*13-_ShowClock*1.7)),12);
                    colour+=glow*wave*(.12+_GlyphIntensity*.18);
                }
                float distanceFade=exp(-length(i.world-GetCameraPositionWS())*.013);
                colour=lerp(float3(.012,.024,.039),colour,distanceFade);
                if(_RealmEntrance41<.9999){
                    float3 q=frac(floor(i.world*6)*.1031);q+=dot(q,q.yzx+33.33);
                    float grain=frac((q.x+q.y)*q.z);
                    float field=saturate(length(i.world-float3(0,1.7,6.35))/38)*.98+grain*.02;
                    clip(_RealmEntrance41*1.05-.025-field);
                }
                if(_RealmDissolve>.00001){
                    float3 q=frac(floor(i.world*6)*.1031);q+=dot(q,q.yzx+33.33);
                    float grain=frac((q.x+q.y)*q.z);
                    float field=min(.98,length((i.world-float3(0,1.5,2))*float3(1,.75,1))/30*.98)+grain*.02;
                    float threshold=_RealmDissolve*1.05-.025;clip(field-threshold);
                    colour+=(1-smoothstep(0,.012,field-threshold))*smoothstep(0,.08,_RealmDissolve)*float3(.15,.65,.95);
                }
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
