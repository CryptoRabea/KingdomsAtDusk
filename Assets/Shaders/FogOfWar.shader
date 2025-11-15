Shader "KingdomsAtDusk/FogOfWar"
{
    Properties
    {
        _FogTex ("Fog Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+100"
            "IgnoreProjector"="True"
        }

        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _FogTex;
            float4 _FogTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _FogTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the fog texture
                fixed4 fogColor = tex2D(_FogTex, i.uv);

                // Apply tint color
                fogColor *= _Color;

                // Apply fog (Unity's built-in fog)
                UNITY_APPLY_FOG(i.fogCoords, fogColor);

                return fogColor;
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
