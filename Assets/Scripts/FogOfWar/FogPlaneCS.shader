Shader "FogOfWar/WorldFogPlane"
{
    Properties
    {
        _MainTex ("Fog Mask (R = Visibility)", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.05, 0.07, 0.1, 1)
        _GlobalAlpha ("Global Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+10"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        // 🔑 DO NOT WRITE DEPTH
        ZWrite Off

        // 🔑 DO NOT TEST DEPTH (prevents blocking view)
        ZTest Always

        // 🔑 Proper alpha blending
        Blend SrcAlpha OneMinusSrcAlpha

        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _FogColor;
            float _GlobalAlpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Fog texture:
                // R = visibility (1 = revealed, 0 = hidden)
                float visibility = tex2D(_MainTex, i.uv).r;

                // Invert because fog = hidden
                float fogAlpha = (1.0 - visibility) * _GlobalAlpha;

                // Kill fully transparent pixels early
                clip(fogAlpha - 0.001);

                return fixed4(
                    _FogColor.rgb,
                    fogAlpha * _FogColor.a
                );
            }
            ENDCG
        }
    }
}
