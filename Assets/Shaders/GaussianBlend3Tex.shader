Shader "UI/GrayscaleGaussianBlend3Tex_UI"
{
    Properties
    {
        _Tex0 ("Tex0", 2D) = "black" {}
        _Tex1 ("Tex1", 2D) = "black" {}
        _Tex2 ("Tex2", 2D) = "black" {}

        _W0 ("W0", Float) = 0
        _W1 ("W1", Float) = 1
        _W2 ("W2", Float) = 0

        _C0 ("C0", Color) = (1,1,1,1)
        _C1 ("C1", Color) = (1,1,1,1)
        _C2 ("C2", Color) = (1,1,1,1)

        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _Tex0;
            sampler2D _Tex1;
            sampler2D _Tex2;

            float _W0, _W1, _W2;
            fixed4 _C0, _C1, _C2;
            fixed4 _Color;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c0 = tex2D(_Tex0, i.uv) * _C0;
                fixed4 c1 = tex2D(_Tex1, i.uv) * _C1;
                fixed4 c2 = tex2D(_Tex2, i.uv) * _C2;

                float g0 = dot(c0.rgb, float3(0.2126, 0.7152, 0.0722));
                float g1 = dot(c1.rgb, float3(0.2126, 0.7152, 0.0722));
                float g2 = dot(c2.rgb, float3(0.2126, 0.7152, 0.0722));

                float outGray = g0 * _W0 + g1 * _W1 + g2 * _W2;

                // 这里用 UI tint 的 alpha（否则 Mask/Canvas 叠加会怪）
                float outA = i.color.a;

                fixed4 outCol = fixed4(outGray, outGray, outGray, outA);
                return outCol * i.color;
            }
            ENDCG
        }
    }
}
