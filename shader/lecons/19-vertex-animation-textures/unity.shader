Shader "Cours/19_VertexAnimationTexture"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        _PositionsCuites ("Positions cuites", 2D) = "black" {}
        _NormalesCuites ("Normales cuites", 2D) = "black" {}
        _NombreImages ("Nombre d'images", Int) = 60
        _ImagesParSeconde ("Images par seconde", Range(0, 120)) = 30
        _BorneMin ("Borne minimum", Vector) = (-1, -1, -1, 0)
        _BorneMax ("Borne maximum", Vector) = (1, 1, 1, 0)
        [Toggle] _Interpoler ("Interpoler entre images", Float) = 1
        _DecalageInstance ("Decalage par instance", Range(0, 1)) = 0
        _Rugosite ("Rugosite", Range(0, 1)) = 0.7
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
            Name "VAT"
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
                float2 uv : TEXCOORD0;
                uint sommetID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PositionsCuites);
            TEXTURE2D(_NormalesCuites);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BorneMin;
                float4 _BorneMax;
                int _NombreImages;
                float _ImagesParSeconde;
                float _Interpoler;
                float _DecalageInstance;
                float _Rugosite;
            CBUFFER_END

            float3 LirePosition(uint sommet, int image)
            {
                float3 brut = LOAD_TEXTURE2D(_PositionsCuites, uint2(sommet, image)).xyz;
                return lerp(_BorneMin.xyz, _BorneMax.xyz, brut);
            }

            float3 LireNormale(uint sommet, int image)
            {
                return LOAD_TEXTURE2D(_NormalesCuites, uint2(sommet, image)).xyz * 2.0 - 1.0;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float total = (float)_NombreImages;
                float curseur = _Time.y * _ImagesParSeconde + _DecalageInstance * total;

                int imageA = (int)fmod(floor(curseur), total);
                int imageB = (int)fmod(floor(curseur) + 1.0, total);
                float melange = frac(curseur) * _Interpoler;

                float3 positionOS = lerp(LirePosition(IN.sommetID, imageA), LirePosition(IN.sommetID, imageB), melange);
                float3 normaleOS = normalize(lerp(LireNormale(IN.sommetID, imageA), LireNormale(IN.sommetID, imageB), melange));

                VertexPositionInputs positions = GetVertexPositionInputs(positionOS);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(normaleOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
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
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                surface.metallic = 0.0;
                surface.smoothness = 1.0 - _Rugosite;
                surface.occlusion = 1.0;
                surface.alpha = 1.0;

                half4 couleur = UniversalFragmentPBR(donnees, surface);
                couleur.rgb = MixFog(couleur.rgb, IN.fogCoord);
                return couleur;
            }
            ENDHLSL
        }
    }
}
