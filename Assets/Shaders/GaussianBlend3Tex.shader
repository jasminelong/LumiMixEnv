Shader "UI/GrayscaleGaussianBlend3Tex"
{
    Properties
    {
        _Tex0 ("Tex0", 2D) = "black" {}
        _Tex1 ("Tex1", 2D) = "black" {}
        _Tex2 ("Tex2", 2D) = "black" {}

        _W0 ("W0", Float) = 0
        _W1 ("W1", Float) = 1
        _W2 ("W2", Float) = 0

        // 可选：想保留你旧的 color 乘法也行
        _C0 ("C0", Color) = (1,1,1,1)
        _C1 ("C1", Color) = (1,1,1,1)
        _C2 ("C2", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Cull Off
        ZWrite Off
        Lighting Off

        // 如果你要让 RawImage 的 alpha 真正参与 UI 混合，取消下面这行注释
        // Blend SrcAlpha OneMinusSrcAlpha

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
                fixed4 c0 = tex2D(_Tex0, i.uv) * _C0;
                fixed4 c1 = tex2D(_Tex1, i.uv) * _C1;
                fixed4 c2 = tex2D(_Tex2, i.uv) * _C2;

                // 灰度（和你原来一样）
                float g0 = dot(c0.rgb, float3(0.2126, 0.7152, 0.0722));
                float g1 = dot(c1.rgb, float3(0.2126, 0.7152, 0.0722));
                float g2 = dot(c2.rgb, float3(0.2126, 0.7152, 0.0722));

                // 用外部传入的 Gaussian 权重混合（权重最好在 C# 已经归一化）
                float outGray = g0 * _W0 + g1 * _W1 + g2 * _W2;

                // alpha：如果你只是全屏显示，直接 1 就最稳，不会出现“透明导致变暗/叠加怪”
                float outA = 1.0;

                return fixed4(outGray, outGray, outGray, outA);
            }
            ENDCG
        }
    }
}
