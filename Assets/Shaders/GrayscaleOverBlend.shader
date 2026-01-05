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
            
            float _EnableAmpNorm;
            float _DEffRad;
            float _Eps;
            float _GainCap;

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

            // fixed4 frag (v2f i) : SV_Target
            // {
            //     fixed4 top = tex2D(_TopTex, i.uv) * _TopColor;
            //     fixed4 bottom = tex2D(_BottomTex, i.uv) * _BottomColor;

            //     float topGray = dot(top.rgb, float3(0.2126, 0.7152, 0.0722));
            //     float bottomGray = dot(bottom.rgb, float3(0.2126, 0.7152, 0.0722));

            //     float topA = top.a;
            //     float bottomA = bottom.a;

            //     float outGray = topGray * topA + bottomGray * (1.0 - topA);
            //     float outAlpha = topA + bottomA * (1.0 - topA);

            //     return fixed4(outGray, outGray, outGray, outAlpha);
            // }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 top = tex2D(_TopTex, i.uv) * _TopColor;
                fixed4 bottom = tex2D(_BottomTex, i.uv) * _BottomColor;

                float topGray = dot(top.rgb, float3(0.2126, 0.7152, 0.0722));
                float bottomGray = dot(bottom.rgb, float3(0.2126, 0.7152, 0.0722));

                float w = top.a;          // 就是你的 topA
                float bottomA = bottom.a; // 仅用于 outAlpha（保持你原逻辑）

                float outGray = topGray * w + bottomGray * (1.0 - w);

                // ===== Amplitude normalization (理论式 A(w,d)) =====
                if (_EnableAmpNorm > 0.5)
{
    float c = cos(_DEffRad);
    float A2 = (1.0 - w)*(1.0 - w) + w*w + 2.0*w*(1.0 - w)*c;
    float A  = sqrt(max(0.0, A2));

    float gain = min(_GainCap, 1.0 / max(_Eps, A));

    // ✅ 用每像素均值作为“局部基准”，避免自然图整体偏暗导致全黑
    float baseGray = 0.5 * (topGray + bottomGray);

    // ✅ 只放大对比度分量 + 软压缩，避免 saturate 大面积夹断
    float delta = (outGray - baseGray) * gain;
    delta = delta / (1.0 + abs(delta));   // soft clip
    outGray = baseGray + delta;
}

outGray = saturate(outGray);
return fixed4(outGray, outGray, outGray, 1.0);   // 调试期建议 alpha=1
            }


            ENDCG
        }
    }
}
