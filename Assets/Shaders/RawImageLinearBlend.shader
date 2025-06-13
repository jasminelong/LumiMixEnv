 Shader "UI/RawImageLinearBlend"
{
    Properties
    {
        _MainTex("Unused (Required for RawImage)", 2D) = "white" {} // ✅ 添加
        _TopTex("Top Texture", 2D) = "white" {}
        _BottomTex("Bottom Texture", 2D) = "black" {}
        _BlendRatio("Blend Ratio (Top 0~1)", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        ZWrite Off
        Cull Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            sampler2D _MainTex;    //  添加（为 RawImage 避免报错）
            sampler2D _TopTex;
            sampler2D _BottomTex;
            float _BlendRatio;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 top = tex2D(_TopTex, i.uv);
                fixed4 bottom = tex2D(_BottomTex, i.uv);
                fixed3 blendedRGB = lerp(bottom.rgb, top.rgb, _BlendRatio);
                return fixed4(blendedRGB, 1.0); // alpha 固定为 1，完全不透明
            }
            ENDCG
        }
    }
}
