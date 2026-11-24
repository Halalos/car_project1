Shader "Cars/SynthwaveSky"
{
    Properties
    {
        _Zenith ("Night", Color) = (0.012, 0.008, 0.045, 1)
        _Horizon ("Horizon", Color) = (0.19, 0.025, 0.24, 1)
        _Sun ("Sun", Color) = (1, 0.18, 0.46, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            float4 _Zenith, _Horizon, _Sun;
            v2f vert(appdata v) { v2f o; o.position = TransformObjectToHClip(v.vertex.xyz); o.direction = v.vertex.xyz; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            half4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float3 color = lerp(_Horizon.rgb, _Zenith.rgb, saturate(d.y*2.4));
                float3 sunDirection = normalize(float3(.2,.13,1));
                float disc = (1-smoothstep(.115,.12,distance(d,sunDirection)));
                color += _Sun.rgb * .16 * exp(-distance(d,sunDirection)*8);
                float bands = step(.24,frac((d.y+.5)*65));
                color = lerp(color,_Sun.rgb*1.25,disc*bands);
                float2 uv = float2(atan2(d.x,d.z)*40,asin(d.y)*65);
                float star = step(.993,hash(floor(uv))) * pow(saturate(1-length(frac(uv)-.5)*2),8) * step(.15,d.y);
                color += star * float3(.5,.7,1);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}

