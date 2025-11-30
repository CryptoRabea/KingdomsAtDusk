Shader "FogWar/FogPlane_URP_Perf"
{
    Properties
    {
        _MainTex("Fog Texture (R = reveal)", 2D) = "white" {}
        _Color("Tint Color", Color) = (0.02, 0.06, 0.12, 1)
        _BlurOffset("Blur Offset (cheap)", Range(0,8)) = 1
        _RevealThreshold("Reveal Threshold", Range(0,1)) = 0.1
        _RevealSoftness("Reveal Softness", Range(0.001,1)) = 0.2

        _HeightFadeStart("Height Fade Start (world Y)", Float) = 0.0
        _HeightFadeEnd("Height Fade End (world Y)", Float) = 5.0

        _WorldFadeStart("World-edge fade start (meters)", Float) = -999
        _WorldFadeEnd("World-edge fade end (meters)", Float) = 999

        // Optional control for additional darkness under fog
        _DarkenAmount("Darken Amount", Range(0,1)) = 0.5
    }

    SubShader
    {
        // Render before transparent vegetation, write depth so it occludes
        Tags { "Queue" = "AlphaTest-1" "RenderType"="Transparent" }

        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height
            float4 _Color;
            float _BlurOffset;
            float _RevealThreshold;
            float _RevealSoftness;
            float _HeightFadeStart;
            float _HeightFadeEnd;
            float _WorldFadeStart;
            float _WorldFadeEnd;
            float _DarkenAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                float4 worldP = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldP.xyz;
                return o;
            }

            // cheap 3x3 gaussian-like (integer weights) - still fast on fragment shader
            inline float SampleBlurredReveal(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy;
                float2 offs = texel * _BlurOffset;

                // 9 samples, integer weights to avoid extra instructions
                float sum = 0.0;
                sum += tex2D(_MainTex, uv + float2(-offs.x, -offs.y)).r * 1.0;
                sum += tex2D(_MainTex, uv + float2(0.0, -offs.y)).r * 2.0;
                sum += tex2D(_MainTex, uv + float2(offs.x, -offs.y)).r * 1.0;

                sum += tex2D(_MainTex, uv + float2(-offs.x, 0.0)).r * 2.0;
                sum += tex2D(_MainTex, uv + float2(0.0, 0.0)).r * 4.0;
                sum += tex2D(_MainTex, uv + float2(offs.x, 0.0)).r * 2.0;

                sum += tex2D(_MainTex, uv + float2(-offs.x, offs.y)).r * 1.0;
                sum += tex2D(_MainTex, uv + float2(0.0, offs.y)).r * 2.0;
                sum += tex2D(_MainTex, uv + float2(offs.x, offs.y)).r * 1.0;

                return sum * (1.0 / 16.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1) Sample blurred reveal value (0 = hidden, 1 = revealed)
                float reveal = SampleBlurredReveal(i.uv);

                // 2) Soft threshold to produce smooth edges
                // move range so that values below threshold are hidden and above become visible
                float soft = _RevealSoftness;
                float alphaFromReveal = saturate((reveal - _RevealThreshold) / max(soft, 0.0001));

                // 3) Height-based fade: if object is above the upper height, fog fades out
                // We produce a factor in [0,1] where 0 = no fade (full fog), 1 = fully faded away
                float heightFade = 0.0;
                if (_HeightFadeEnd != _HeightFadeStart)
                {
                    heightFade = saturate((i.worldPos.y - _HeightFadeStart) / (_HeightFadeEnd - _HeightFadeStart));
                }
                // want fog less where heightFade ~ 1 (so final alpha is multiplied by (1 - heightFade))
                float heightMultiplier = 1.0 - heightFade;

                // 4) Optional world-bound fade (for edges of world / level)
                float worldFade = 1.0;
                if (_WorldFadeEnd > _WorldFadeStart)
                {
                    // distance from plane center is assumed encoded by UV; we can also use world XZ if needed.
                    // Here we use distance from uv center (0.5,0.5) as a cheap world-edge fade - user can set wide range to disable.
                    float2 uvCenter = float2(0.5, 0.5);
                    float dist = distance(i.uv, uvCenter) * max(1.0, 1.0); // scale can be applied externally if needed
                    worldFade = saturate(1.0 - (dist - _WorldFadeStart) / max(0.0001, (_WorldFadeEnd - _WorldFadeStart)));
                }

                // 5) Compose final alpha: fog alpha is high where reveal is low (we invert reveal)
                float fogAlpha = (1.0 - alphaFromReveal) * heightMultiplier * worldFade;

                // optionally darken the underlying scene slightly inside fog
                float finalDark = lerp(1.0, 1.0 - _DarkenAmount, fogAlpha);

                fixed4 col = _Color;
                col.rgb *= finalDark;
                col.a = saturate(fogAlpha * _Color.a);

                return col;
            }

            ENDHLSL
        }
    }
}
