Shader "CounterAttack/Ball Top Face"
{
    Properties
    {
        _BaseColor ("Ball colour", Color) = (1, 1, 1, 1)
        _FacePattern ("Face pattern", 2D) = "white" {}
        _FaceBounds ("Mesh XZ centre and size", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _FacePattern;
            half4 _BaseColor;
            float4 _FaceBounds;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 faceUV : TEXCOORD0;
                half3 normalOS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.faceUV = (input.positionOS.xz - _FaceBounds.xy) / _FaceBounds.zw + 0.5;
                output.faceUV = (output.faceUV - 0.5) / 0.76 + 0.5;
                output.normalOS = input.normalOS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half light = lerp(0.72, 1.0, saturate(normalize(input.normalOS).y));
                half3 pattern = tex2D(_FacePattern, saturate(input.faceUV)).rgb;
                half luminance = dot(pattern, half3(0.2126, 0.7152, 0.0722));
                half inside = all(input.faceUV >= 0) && all(input.faceUV <= 1) ? 1 : 0;
                half opacity = (1 - smoothstep(0.15, 0.85, luminance)) * inside
                    * smoothstep(0.05, 0.55, normalize(input.normalOS).y);
                return half4(lerp(_BaseColor.rgb * light, half3(0.015, 0.015, 0.015), opacity), 1.0);
            }
            ENDCG
        }
    }
}
