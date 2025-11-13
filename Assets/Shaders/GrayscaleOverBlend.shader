Shader "UI/GrayscaleOverBlend"
{
    Properties
    {
        _TopTex ("Top Texture", 2D) = "white" {}
        _BottomTex ("Bottom Texture", 2D) = "black" {}
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TopTex;
            sampler2D _BottomTex;
            fixed4 _TopColor;
            fixed4 _BottomColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 top = tex2D(_TopTex, i.uv) * _TopColor;
                fixed4 bottom = tex2D(_BottomTex, i.uv) * _BottomColor;

                float topGray = dot(top.rgb, float3(0.2126, 0.7152, 0.0722));
                float bottomGray = dot(bottom.rgb, float3(0.2126, 0.7152, 0.0722));

                float topA = top.a;
                float bottomA = bottom.a;

                float outGray = topGray * topA + bottomGray * (1.0 - topA);
                float outAlpha = topA + bottomA * (1.0 - topA);

                return fixed4(outGray, outGray, outGray, outAlpha);
            }
/*             fixed4 frag (v2f i) : SV_Target
{
    fixed4 top = tex2D(_TopTex, i.uv) * _TopColor;
    fixed4 bottom = tex2D(_BottomTex, i.uv) * _BottomColor;

    // --- 线性灰度
    float topGray = dot(top.rgb, float3(0.2126, 0.7152, 0.0722));
    float bottomGray = dot(bottom.rgb, float3(0.2126, 0.7152, 0.0722));

    float w = saturate(top.a);   // 你用的混合权重

    // === 感知空间混合 ===
    // 人眼亮度近似 P = L^alpha，alpha≈0.45
    const float alpha = 0.45;
    float topP    = pow(topGray,    alpha);
    float bottomP = pow(bottomGray, alpha);
    float mixP    = lerp(bottomP, topP, w);
    float outGray = pow(mixP, 1.0 / alpha);

    float outAlpha = w + bottom.a * (1.0 - w);
    return fixed4(outGray, outGray, outGray, outAlpha);
} */

            ENDCG
        }
    }
}
