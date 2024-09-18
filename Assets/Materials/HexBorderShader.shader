Shader "Custom/HexBorderShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.5, 1, 0.5, 1) // Color of the hexagon
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1) // Color of the border
        _BorderThickness ("Border Thickness", Range(0, 0.2)) = 0.05 // Thickness of the border
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Uniforms (properties) to be set in Unity
            float4 _MainColor;
            float4 _BorderColor;
            float _BorderThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Calculate the distance from the center of the hexagon (flat-topped)
            float hexagonDistance(float2 uv)
            {
                // Center the UV coordinates
                float2 p = 2.0 * uv - 1.0;
                // Shrink horizontally to fit hex grid
                p.x *= 1.1547;

                // Hexagon distance calculation
                p = abs(p);
                return max(p.x * 0.866 + p.y * 0.5, p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Get the distance from the hexagon center
                float dist = hexagonDistance(i.uv);

                // Apply the border color near the edges
                if (dist > 0.5 - _BorderThickness)
                {
                    return _BorderColor;
                }
                else
                {
                    return _MainColor;
                }
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
