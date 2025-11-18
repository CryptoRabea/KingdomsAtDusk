Shader "KingdomsAtDusk/FogOfWarCameraEffect"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _FogTex ("Fog Texture", 2D) = "white" {}
        _DimStrength ("Dim Strength", Range(0, 1)) = 0.7
        _WorldBoundsMin ("World Bounds Min", Vector) = (-1000, 0, -1000, 0)
        _WorldBoundsMax ("World Bounds Max", Vector) = (1000, 0, 1000, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always
        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
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
                float2 uv : TEXCOORD0;
                float3 ray : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _FogTex;
            sampler2D_float _CameraDepthTexture;
            float4 _MainTex_ST;
            float _DimStrength;
            float4 _WorldBoundsMin;
            float4 _WorldBoundsMax;

            // Camera matrices
            float4x4 _InverseView;
            float4x4 _InverseProjection;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Create ray from camera through this vertex
                float4 clipPos = float4(v.uv * 2.0 - 1.0, 1.0, 1.0);
                float4 viewPos = mul(unity_CameraInvProjection, clipPos);
                o.ray = mul(unity_CameraToWorld, float4(viewPos.xyz, 0.0)).xyz;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the original camera output
                fixed4 color = tex2D(_MainTex, i.uv);

                // Get depth
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = LinearEyeDepth(depth);

                // Reconstruct world position from depth
                float3 viewDir = normalize(i.ray);
                float3 camPos = _WorldSpaceCameraPos;
                float3 worldPos = camPos + viewDir * linearDepth;

                // Map world position to fog texture UV (using XZ plane for top-down)
                float2 fogUV;
                fogUV.x = saturate((worldPos.x - _WorldBoundsMin.x) / (_WorldBoundsMax.x - _WorldBoundsMin.x));
                fogUV.y = saturate((worldPos.z - _WorldBoundsMin.z) / (_WorldBoundsMax.z - _WorldBoundsMin.z));

                // Sample fog texture
                fixed4 fogColor = tex2D(_FogTex, fogUV);

                // Apply dimming: darken the color based on fog alpha
                // Higher fog alpha = more dimming
                float dimAmount = fogColor.a * _DimStrength;
                color.rgb *= (1.0 - dimAmount);

                return color;
            }
            ENDCG
        }
    }

    FallBack Off
}
