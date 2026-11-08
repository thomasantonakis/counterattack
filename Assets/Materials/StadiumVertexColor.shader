Shader "CounterAttack/Stadium Vertex Color"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _PaletteEnabled ("Use Match Colours", Float) = 0
        _SeatColor ("Seat Colour", Color) = (1,1,1,1)
        _OutfitTex ("Spectator Outfits", 2D) = "white" {}
        _OutfitWidth ("Outfit Texture Width", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _Color;
        fixed4 _SeatColor;
        float _PaletteEnabled, _OutfitWidth;
        sampler2D _OutfitTex;
        struct Input { fixed4 vertexColor; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertexColor = v.color;
            // Authored UV0: x = spectator index, y = body-part role.
            // Resolve only at vertices; shared scene meshes remain unchanged.
            if (_PaletteEnabled > 0.5)
            {
                if (v.texcoord.y > 2.5) o.vertexColor = _SeatColor;
                else if (v.texcoord.y > 0.5)
                    o.vertexColor = tex2Dlod(_OutfitTex, float4((v.texcoord.x + 0.5) / _OutfitWidth,
                        v.texcoord.y > 1.5 ? 0.75 : 0.25, 0, 0));
            }
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.vertexColor.rgb * _Color.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
