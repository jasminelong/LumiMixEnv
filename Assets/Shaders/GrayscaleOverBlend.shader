Shader "UI/GrayscaleOverBlend_FixWhite_Improved"
{
    Properties
    {
        _TopTex ("Top Texture", 2D) = "white" {}
        _BottomTex ("Bottom Texture", 2D) = "black" {}

        // 用 _TopColor.a 传 w (0..1)
        _TopColor ("Top Color (A = w)", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)

        // --- AmpNorm params ---
        _EnableAmpNorm ("Enable AmpNorm", Float) = 0
        _DEffRad ("dEff (rad)", Float) = 2.827433  // 0.9*pi
        _Eps ("amp_eps", Float) = 0.08
        _GainCap ("gain_cap", Float) = 2.5

        // ✅ 0: base=0.5 const（grating最推荐，最像Python）
        //    1: base=avg(top,bottom)
        //    2: base=bottom
        _BaseMode ("baseMode 0=0.5 1=avg 2=bottom", Float) = 0

        // ✅ 对齐 Python 的 to_img_gray(scale=2.5)
        _DispScale ("disp_scale (Python=2.5)", Float) = 2.5

        _SoftClip ("softclip (0=off, 1=on)", Float) = 1

        // 这两个先留着（你现在没用到也无所谓）
        _EdgeK ("edgeK", Float) = 0.01
        _EdgePow ("edgePow", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        // 不用 alpha 混背景，避免整体偏白
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TopTex;
            sampler2D _BottomTex;
            float4 _TopTex_ST;

            fixed4 _TopColor;
            fixed4 _BottomColor;

            float _EnableAmpNorm;
            float _DEffRad;
            float _Eps;
            float _GainCap;
            float _BaseMode;
            float _DispScale;
            float _SoftClip;
            float _EdgeK;
            float _EdgePow;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _TopTex);
                o.color = v.color;
                return o;
            }

            // cross-dissolve 的振幅 A(w,d)
            float MixAmplitude(float w, float dEffRad)
            {
                float c  = cos(dEffRad);
                float om = (1.0 - w);
                float A2 = om*om + w*w + 2.0*w*om*c;
                return sqrt(max(0.0, A2));
            }

            float GainFromAmplitude(float A, float eps, float cap)
            {
                float g = 1.0 / max(eps, A);
                return min(g, cap);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 采样（Unity会按纹理sRGB/Linear标记自动解码）
                fixed3 topRGB = tex2D(_TopTex, i.uv).rgb * _TopColor.rgb;
                fixed3 botRGB = tex2D(_BottomTex, i.uv).rgb * _BottomColor.rgb;

                // 灰度
                float topG = dot(topRGB, float3(0.3333333, 0.3333333, 0.3333333));
                float botG = dot(botRGB, float3(0.3333333, 0.3333333, 0.3333333));

                // w
                float w = saturate(_TopColor.a);

                // 基础混合
                float mixG = lerp(botG, topG, w);

                if (_EnableAmpNorm > 0.5)
                {
                    // ✅ base 选择（grating 用 base=0.5 最像 Python）
                    float baseG;
                    if (_BaseMode < 0.5)        baseG = 0.5;
                    else if (_BaseMode < 1.5)   baseG = 0.5 * (topG + botG);
                    else                        baseG = botG;

                    float A    = MixAmplitude(w, _DEffRad);
                    float gain = GainFromAmplitude(A, _Eps, _GainCap);

                    // ✅ 对齐 Python：gain 后再除以 disp_scale，避免“突然整体偏白/硬clip”
                    float gEff = gain / max(_DispScale, 1e-6);

                    float sig = (mixG - baseG) * gEff + baseG;
                    mixG = (_SoftClip > 0.5) ? saturate(sig) : sig;
                }

                float outG = saturate(mixG);
                return fixed4(outG, outG, outG, 1.0); // 强制alpha=1
            }
            ENDCG
        }
    }
}
