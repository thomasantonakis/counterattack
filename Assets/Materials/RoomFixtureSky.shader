Shader "CounterAttack/Room Fixture Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (.025,.055,.13,1)
        _Horizon ("Horizon", Color) = (.12,.15,.24,1)
        _Ground ("Ground", Color) = (.015,.02,.035,1)
        _Glow ("Horizon glow", Color) = (.18,.14,.10,1)
        _GlowStrength ("Glow strength", Range(0,1)) = .1
        _Stars ("Stars", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            float4 _Zenith, _Horizon, _Ground, _Glow;
            float _GlowStrength, _Stars;
            v2f vert(appdata v)
            {
                v2f o; o.position = UnityObjectToClipPos(v.vertex); o.direction = v.vertex.xyz; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float3 sky = lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(d.y), .55));
                sky = lerp(sky, _Ground.rgb, saturate(-d.y * 5));
                float glow = exp(-abs(d.y - .025) * 18) * pow(saturate(dot(d.xz, normalize(float2(-.5,.86)))), 6);
                sky += _Glow.rgb * glow * _GlowStrength;
                // Fixed directional cells: stars stay still when the camera moves.
                float3 cell = floor(d * 420);
                float hash = frac(sin(dot(cell,float3(12.9898,78.233,37.719))) * 43758.5453);
                float star = smoothstep(.9986,.9999,hash) * smoothstep(.08,.3,d.y);
                sky += star * _Stars * .32;
                return fixed4(sky,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
