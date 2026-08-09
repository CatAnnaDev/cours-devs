Shader "Cours/04_MasquesEtMelanges"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        _Bruit ("Bruit", 2D) = "gray" {}
        _CouleurNeige ("Couleur neige", Color) = (0.90, 0.93, 1.0, 1.0)
        _Couverture ("Couverture", Range(0, 1)) = 0.5
        _Nettete ("Nettete", Range(0.002, 0.5)) = 0.12
        _Irregularite ("Irregularite", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Bruit);
            SAMPLER(sampler_Bruit);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Bruit_ST;
                float4 _CouleurNeige;
                float _Couverture;
                float _Nettete;
                float _Irregularite;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;

                float3 normalWS = normalize(IN.normalWS);
                float versLeHaut = normalWS.y * 0.5 + 0.5;

                half grain = SAMPLE_TEXTURE2D(_Bruit, sampler_Bruit, IN.uv * 1.7).r;
                float valeur = versLeHaut - _Irregularite * (1.0 - grain);

                float seuil = 1.0 - _Couverture;
                float masque = smoothstep(seuil - _Nettete, seuil + _Nettete, valeur);

                return half4(lerp(base, _CouleurNeige.rgb, masque), 1.0);
            }
            ENDHLSL
        }
    }
}
