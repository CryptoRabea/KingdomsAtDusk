Shader "KingdomsAtDusk/FogOfWarURP"
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
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Blend Off
        Cull Off

        Pass
        {
            Name "FogOfWarPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewRay : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);

            float4 _MainTex_ST;
            float _DimStrength;
            float4 _WorldBoundsMin;
            float4 _WorldBoundsMax;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // Create view ray for depth reconstruction
                float3 viewPos = ComputeViewSpacePosition(output.uv, 1.0, unity_CameraInvProjection);
                output.viewRay = mul(unity_MatrixInvV, float4(viewPos, 0.0)).xyz;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample original color
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Sample depth
                float depth = SampleSceneDepth(input.uv);

                // Skip skybox
                #if UNITY_REVERSED_Z
                    if (depth < 0.0001)
                        return color;
                #else
                    if (depth > 0.9999)
                        return color;
                #endif

                // Linearize depth
                depth = LinearEyeDepth(depth, _ZBufferParams);

                // Reconstruct world position
                float3 worldPos = _WorldSpaceCameraPos + normalize(input.viewRay) * depth;

                // Calculate fog UV from world position (XZ plane for top-down)
                float2 fogUV;
                fogUV.x = saturate((worldPos.x - _WorldBoundsMin.x) / (_WorldBoundsMax.x - _WorldBoundsMin.x));
                fogUV.y = saturate((worldPos.z - _WorldBoundsMin.z) / (_WorldBoundsMax.z - _WorldBoundsMin.z));

                // Sample fog texture
                half4 fogColor = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUV);

                // Apply dimming
                float dimAmount = fogColor.a * _DimStrength;
                color.rgb *= (1.0 - dimAmount);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
