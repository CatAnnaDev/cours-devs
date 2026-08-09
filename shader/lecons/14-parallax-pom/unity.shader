Shader "Cours/14_ParallaxPOM"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        [Normal] _Normales ("Normal map", 2D) = "bump" {}
        _CarteHauteur ("Carte de hauteur", 2D) = "white" {}
        _Carrelage ("Carrelage", Vector) = (1, 1, 0, 0)
        _Profondeur ("Profondeur", Range(0, 0.3)) = 0.06
        _CouchesMin ("Couches minimum", Range(4, 64)) = 8
        _CouchesMax ("Couches maximum", Range(4, 128)) = 32
        _Rugosite ("Rugosite", Range(0, 1)) = 0.85
        [Toggle] _CouperLesBords ("Couper les bords", Float) = 0
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
            Name "Parallax"
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
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float fogCoord : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Normales);
            SAMPLER(sampler_Normales);
            TEXTURE2D(_CarteHauteur);
            SAMPLER(sampler_CarteHauteur);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Normales_ST;
                float4 _CarteHauteur_ST;
                float4 _Carrelage;
                float _Profondeur;
                float _CouchesMin;
                float _CouchesMax;
                float _Rugosite;
                float _CouperLesBords;
            CBUFFER_END

            float Hauteur(float2 uv)
            {
                return 1.0 - SAMPLE_TEXTURE2D(_CarteHauteur, sampler_CarteHauteur, uv).r;
            }

            float2 ParallaxeOcclusion(float2 uvDepart, float3 vue)
            {
                float couches = lerp(_CouchesMax, _CouchesMin, saturate(abs(vue.z)));
                float pasProfondeur = 1.0 / couches;
                float2 pasUV = vue.xy * _Profondeur / couches;

                float profondeurCourante = 0.0;
                float2 uv = uvDepart;
                float hauteur = Hauteur(uv);

                [loop]
                for (int i = 0; i < 128; i++)
                {
                    if (profondeurCourante >= hauteur || (float)i >= couches)
                    {
                        break;
                    }
                    uv -= pasUV;
                    hauteur = Hauteur(uv);
                    profondeurCourante += pasProfondeur;
                }

                float2 uvPrecedent = uv + pasUV;
                float ecartApres = hauteur - profondeurCourante;
                float ecartAvant = Hauteur(uvPrecedent) - profondeurCourante + pasProfondeur;
                float poids = ecartApres / max(ecartApres - ecartAvant, 0.0001);

                return lerp(uv, uvPrecedent, saturate(poids));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normales = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = normales.normalWS;
                OUT.tangentWS = normales.tangentWS;
                OUT.bitangentWS = normales.bitangentWS;
                OUT.uv = IN.uv;
                OUT.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3x3 versTangente = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS));

                float3 vueWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float3 vueTS = normalize(mul(versTangente, vueWS));

                float2 uv = ParallaxeOcclusion(IN.uv * _Carrelage.xy, vueTS);

                if (_CouperLesBords > 0.5)
                {
                    clip(float4(uv, 1.0 - uv));
                }

                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, uv));

                InputData donnees = (InputData)0;
                donnees.positionWS = IN.positionWS;
                donnees.normalWS = normalize(mul(normalTS, versTangente));
                donnees.viewDirectionWS = vueWS;
                donnees.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                donnees.bakedGI = SampleSH(donnees.normalWS);
                donnees.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                donnees.shadowMask = half4(1, 1, 1, 1);
                donnees.fogCoord = IN.fogCoord;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
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
