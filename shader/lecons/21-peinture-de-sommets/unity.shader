Shader "Cours/21_PeintureDeSommets"
{
    Properties
    {
        _CoucheRouge ("Couche rouge", 2D) = "white" {}
        _CoucheVerte ("Couche verte", 2D) = "white" {}
        _CoucheBleue ("Couche bleue", 2D) = "white" {}
        _Hauteurs ("Hauteurs (R, G, B)", 2D) = "gray" {}
        _Carrelage ("Carrelage par couche", Vector) = (4, 4, 4, 0)
        [Toggle] _MelangeParHauteur ("Melange par hauteur", Float) = 1
        _Durete ("Durete du melange", Range(0.001, 1)) = 0.15
        _ForceOcclusion ("Force de l'occlusion", Range(0, 1)) = 1.0
        _Rugosite ("Rugosite", Range(0, 1)) = 0.85
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
            Name "PeintureDeSommets"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 peinture : COLOR;
                float fogCoord : TEXCOORD3;
            };

            TEXTURE2D(_CoucheRouge);
            SAMPLER(sampler_CoucheRouge);
            TEXTURE2D(_CoucheVerte);
            SAMPLER(sampler_CoucheVerte);
            TEXTURE2D(_CoucheBleue);
            SAMPLER(sampler_CoucheBleue);
            TEXTURE2D(_Hauteurs);
            SAMPLER(sampler_Hauteurs);

            CBUFFER_START(UnityPerMaterial)
                float4 _CoucheRouge_ST;
                float4 _CoucheVerte_ST;
                float4 _CoucheBleue_ST;
                float4 _Hauteurs_ST;
                float4 _Carrelage;
                float _MelangeParHauteur;
                float _Durete;
                float _ForceOcclusion;
                float _Rugosite;
            CBUFFER_END

            float3 PoidsLineaires(float3 peinture)
            {
                float somme = peinture.r + peinture.g + peinture.b;
                return somme > 0.0001 ? peinture / somme : float3(1, 0, 0);
            }

            float3 PoidsParHauteur(float3 peinture, float3 hauteur)
            {
                float3 combines = peinture * (hauteur + 0.0001);
                float maximum = max(combines.r, max(combines.g, combines.b));
                float3 retenus = max(combines - (maximum - _Durete), 0.0);
                float somme = retenus.r + retenus.g + retenus.b;
                return somme > 0.0001 ? retenus / somme : PoidsLineaires(peinture);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.peinture = IN.color;
                OUT.uv = IN.uv;
                OUT.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 rouge = SAMPLE_TEXTURE2D(_CoucheRouge, sampler_CoucheRouge, IN.uv * _Carrelage.r).rgb;
                half3 verte = SAMPLE_TEXTURE2D(_CoucheVerte, sampler_CoucheVerte, IN.uv * _Carrelage.g).rgb;
                half3 bleue = SAMPLE_TEXTURE2D(_CoucheBleue, sampler_CoucheBleue, IN.uv * _Carrelage.b).rgb;

                float3 hauteur = float3(
                    SAMPLE_TEXTURE2D(_Hauteurs, sampler_Hauteurs, IN.uv * _Carrelage.r).r,
                    SAMPLE_TEXTURE2D(_Hauteurs, sampler_Hauteurs, IN.uv * _Carrelage.g).g,
                    SAMPLE_TEXTURE2D(_Hauteurs, sampler_Hauteurs, IN.uv * _Carrelage.b).b);

                float3 lineaires = PoidsLineaires(IN.peinture.rgb);
                float3 parHauteur = PoidsParHauteur(IN.peinture.rgb, hauteur);
                float3 poids = lerp(lineaires, parHauteur, _MelangeParHauteur);

                half3 melange = rouge * poids.r + verte * poids.g + bleue * poids.b;

                InputData donnees = (InputData)0;
                donnees.positionWS = IN.positionWS;
                donnees.normalWS = normalize(IN.normalWS);
                donnees.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                donnees.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                donnees.bakedGI = SampleSH(donnees.normalWS);
                donnees.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                donnees.shadowMask = half4(1, 1, 1, 1);
                donnees.fogCoord = IN.fogCoord;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = melange;
                surface.metallic = 0.0;
                surface.smoothness = 1.0 - _Rugosite;
                surface.occlusion = lerp(1.0, IN.peinture.a, _ForceOcclusion);
                surface.alpha = 1.0;

                half4 couleur = UniversalFragmentPBR(donnees, surface);
                couleur.rgb = MixFog(couleur.rgb, IN.fogCoord);
                return couleur;
            }
            ENDHLSL
        }
    }
}
