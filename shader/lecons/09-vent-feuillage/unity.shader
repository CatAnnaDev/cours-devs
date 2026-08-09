Shader "Cours/09_VentFeuillage"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        _SeuilAlpha ("Seuil alpha", Range(0, 1)) = 0.5
        _DirectionVent ("Direction du vent", Vector) = (1, 0.3, 0, 0)
        _ForceVent ("Force du vent", Range(0, 1)) = 0.15
        _FrequenceVent ("Frequence du vent", Range(0, 4)) = 1.2
        _LongueurRafale ("Longueur de rafale", Range(0.5, 40)) = 8.0
        _ForceFeuille ("Force du frisson", Range(0, 0.2)) = 0.02
        _FrequenceFeuille ("Frequence du frisson", Range(0, 20)) = 6.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _DirectionVent;
            float _SeuilAlpha;
            float _ForceVent;
            float _FrequenceVent;
            float _LongueurRafale;
            float _ForceFeuille;
            float _FrequenceFeuille;
        CBUFFER_END

        float3 AppliquerVent(float3 positionWS, float souplesse)
        {
            float2 direction = normalize(_DirectionVent.xy);
            float phase = dot(positionWS.xz, direction) / _LongueurRafale;

            float rafale = sin(_Time.y * _FrequenceVent - phase);
            float3 poussee = float3(direction.x, 0.0, direction.y) * rafale * _ForceVent;

            float flottement = sin(_Time.y * _FrequenceFeuille + positionWS.x * 3.1 + positionWS.z * 2.3);
            float3 tremblement = float3(0.0, flottement * _ForceFeuille, 0.0);

            return positionWS + (poussee + tremblement) * souplesse;
        }
        ENDHLSL

        Pass
        {
            Name "Feuillage"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS = AppliquerVent(positionWS, IN.color.r);

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 echantillon = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(echantillon.a - _SeuilAlpha);
                return half4(echantillon.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertOmbre
            #pragma fragment fragOmbre

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct AttributsOmbre
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct VariationsOmbre
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VariationsOmbre vertOmbre(AttributsOmbre IN)
            {
                VariationsOmbre OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS = AppliquerVent(positionWS, IN.color.r);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 fragOmbre(VariationsOmbre IN) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a;
                clip(alpha - _SeuilAlpha);
                return 0;
            }
            ENDHLSL
        }
    }
}
