Shader "Cours/08_ToonEtContour"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        _CouleurOmbre ("Couleur d'ombre", Color) = (0.45, 0.42, 0.60, 1.0)
        _Niveaux ("Niveaux", Range(1, 8)) = 3
        _Douceur ("Douceur des marches", Range(0, 0.5)) = 0.03
        _SeuilSpeculaire ("Seuil speculaire", Range(0, 1)) = 0.35
        _ForceSpeculaire ("Force speculaire", Range(0, 4)) = 1.0
        [HDR] _CouleurLisere ("Couleur du lisere", Color) = (1.0, 0.97, 0.90, 1.0)
        _PuissanceLisere ("Puissance du lisere", Range(0.5, 16)) = 6.0
        _ForceLisere ("Force du lisere", Range(0, 2)) = 0.4
        _CouleurContour ("Couleur du contour", Color) = (0.05, 0.03, 0.08, 1.0)
        _Epaisseur ("Epaisseur du contour", Range(0, 0.1)) = 0.01
        [Toggle] _EpaisseurConstante ("Epaisseur constante a l'ecran", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _CouleurOmbre;
            float4 _CouleurLisere;
            float4 _CouleurContour;
            float _Niveaux;
            float _Douceur;
            float _SeuilSpeculaire;
            float _ForceSpeculaire;
            float _PuissanceLisere;
            float _ForceLisere;
            float _Epaisseur;
            float _EpaisseurConstante;
        CBUFFER_END

        float Marches(float valeur, float nombre, float largeur)
        {
            float echelle = valeur * nombre;
            float palier = floor(echelle);
            float reste = frac(echelle);
            return (palier + smoothstep(0.5 - largeur, 0.5 + largeur, reste)) / nombre;
        }
        ENDHLSL

        Pass
        {
            Name "Contour"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertContour
            #pragma fragment fragContour

            struct AttributsContour
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct VariationsContour
            {
                float4 positionCS : SV_POSITION;
            };

            VariationsContour vertContour(AttributsContour IN)
            {
                VariationsContour OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float distanceVue = length(TransformWorldToView(positionWS));
                float echelle = lerp(1.0, distanceVue, _EpaisseurConstante);

                float3 positionOS = IN.positionOS.xyz + normalize(IN.normalOS) * _Epaisseur * echelle;
                OUT.positionCS = TransformObjectToHClip(positionOS);
                return OUT;
            }

            half4 fragContour(VariationsContour IN) : SV_Target
            {
                return half4(_CouleurContour.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Toon"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

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
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 vueWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                float4 coordOmbre = TransformWorldToShadowCoord(IN.positionWS);
                Light lumiere = GetMainLight(coordOmbre);

                float eclairement = dot(normalWS, lumiere.direction) * 0.5 + 0.5;
                eclairement *= lumiere.shadowAttenuation;
                float marche = Marches(eclairement, _Niveaux, _Douceur);

                half3 teinte = lerp(_CouleurOmbre.rgb, half3(1.0, 1.0, 1.0), marche);

                float3 demi = normalize(lumiere.direction + vueWS);
                float brillance = pow(saturate(dot(normalWS, demi)), 32.0);
                float tache = step(_SeuilSpeculaire, brillance) * _ForceSpeculaire;

                float face = saturate(dot(normalWS, vueWS));
                float lisere = pow(1.0 - face, _PuissanceLisere) * _ForceLisere * marche;

                half3 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                half3 couleur = base * teinte * lumiere.color;
                couleur += (tache.xxx + _CouleurLisere.rgb * lisere) * lumiere.color;

                return half4(couleur, 1.0);
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
            };

            struct VariationsOmbre
            {
                float4 positionCS : SV_POSITION;
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
                return OUT;
            }

            half4 fragOmbre(VariationsOmbre IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
