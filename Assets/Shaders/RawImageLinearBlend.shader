Shader "UI/RawImageLinearBlend_LinearToSRGB"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint",    Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"            = "Transparent"
            "IgnoreProjector"  = "True"
            "RenderType"       = "Transparent"
            "CanUseSpriteAtlas"= "False"
        }
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

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;   // RawImage Inspector 上设置的 RGBA

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color * _Color;  
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1) 读贴图，Unity 自动把 _MainTex 从 sRGB → Linear
                float4 texCol = tex2D (_MainTex, i.uv);

                // 2) 用 RawImage Inspector 上的颜色 (i.color) 做一次乘法
                //    （同时也把 i.color 从 sRGB→Linear）
                float4 linCol = texCol * i.color;

                // 3) 做线性混合（因为 Blend SrcAlpha OneMinusSrcAlpha 也是在线性空间生效）
                //    第二 Pass 会自动把 linCol.rgb 视为 Linear，和屏幕缓存做线性混合

                // 4) 手动把线性颜色 “Linear→sRGB” 再转回来，输出给屏幕
                linCol.rgb = LinearToGammaSpace (linCol.rgb);

                return linCol;
            }
            ENDCG
        }
    }
}
