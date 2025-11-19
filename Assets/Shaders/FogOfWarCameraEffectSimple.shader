Shader "KingdomsAtDusk/FogOfWarCameraEffectSimple"
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

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float3 rayDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _FogTex;
            sampler2D_float _CameraDepthTexture;
            float _DimStrength;
            float4 _WorldBoundsMin;
            float4 _WorldBoundsMax;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Create view ray
                float4 clipPos = float4(v.uv * 2.0 - 1.0, 1.0, 1.0);
                clipPos.y *= -1.0; // Flip Y for Unity

                float4 viewPos = mul(unity_CameraInvProjection, clipPos);
                o.rayDir = mul(unity_CameraToWorld, float4(viewPos.xyz / viewPos.w, 0.0)).xyz;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample original color
                fixed4 col = tex2D(_MainTex, i.uv);

                // Get depth
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);

                // Handle skybox/far plane
                if (depth >= 0.9999)
                {
                    return col; // Don't dim skybox
                }

                depth = Linear01Depth(depth);

                // Reconstruct world position
                float3 worldPos = _WorldSpaceCameraPos + i.rayDir * depth * _ProjectionParams.z;

                // Calculate fog UV from world position (XZ plane)
                float2 fogUV;
                fogUV.x = saturate((worldPos.x - _WorldBoundsMin.x) / (_WorldBoundsMax.x - _WorldBoundsMin.x));
                fogUV.y = saturate((worldPos.z - _WorldBoundsMin.z) / (_WorldBoundsMax.z - _WorldBoundsMin.z));

                // Sample fog
                fixed4 fogCol = tex2D(_FogTex, fogUV);

                // Apply dimming
                float dimFactor = 1.0 - (fogCol.a * _DimStrength);
                col.rgb *= dimFactor;

                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
