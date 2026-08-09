Shader "Cours/05_Dissolution"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        _Bruit ("Bruit", 2D) = "gray" {}
        _Progression ("Progression", Range(0, 1)) = 0.0
        _EchelleBruit ("Echelle du bruit", Range(0.5, 16)) = 4.0
        [HDR] _CouleurBord ("Couleur du bord", Color) = (1.0, 0.35, 0.05, 1.0)
        _LargeurBord ("Largeur du bord", Range(0.001, 0.4)) = 0.08
        _IntensiteBord ("Intensite du bord", Range(0, 20)) = 6.0
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
        TEXTURE2D(_Bruit);
        SAMPLER(sampler_Bruit);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _Bruit_ST;
            float4 _CouleurBord;
            float _Progression;
            float _EchelleBruit;
            float _LargeurBord;
            float _IntensiteBord;
        CBUFFER_END

        float PartieVisible(float2 uv)
        {
            float grain = SAMPLE_TEXTURE2D(_Bruit, sampler_Bruit, uv * _EchelleBruit).r;
            float seuil = lerp(-_LargeurBord, 1.0 + _LargeurBord, _Progression);
            return grain - seuil;
        }
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float visible = PartieVisible(IN.uv);
                clip(visible);

                half bord = 1.0 - smoothstep(0.0, _LargeurBord, visible);
                half3 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;

                return half4(base + _CouleurBord.rgb * bord * _IntensiteBord, 1.0);
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
                clip(PartieVisible(IN.uv));
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertProfondeur
            #pragma fragment fragProfondeur

            struct AttributsProfondeur
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VariationsProfondeur
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VariationsProfondeur vertProfondeur(AttributsProfondeur IN)
            {
                VariationsProfondeur OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 fragProfondeur(VariationsProfondeur IN) : SV_Target
            {
                clip(PartieVisible(IN.uv));
                return 0;
            }
            ENDHLSL
        }
    }
}
