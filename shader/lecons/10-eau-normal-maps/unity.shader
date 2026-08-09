Shader "Cours/10_Eau"
{
    Properties
    {
        [Normal] _Normales ("Normal map", 2D) = "bump" {}
        _CouleurEau ("Couleur de l'eau", Color) = (0.05, 0.28, 0.38, 1.0)
        _CarrelageA ("Carrelage couche A", Vector) = (3, 3, 0, 0)
        _CarrelageB ("Carrelage couche B", Vector) = (7, 7, 0, 0)
        _VitesseA ("Vitesse couche A", Vector) = (0.030, 0.018, 0, 0)
        _VitesseB ("Vitesse couche B", Vector) = (-0.021, 0.035, 0, 0)
        _ForceNormales ("Force des normales", Range(0, 3)) = 0.7
        _Rugosite ("Rugosite", Range(0, 1)) = 0.05
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
            Name "Eau"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
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

            TEXTURE2D(_Normales);
            SAMPLER(sampler_Normales);

            CBUFFER_START(UnityPerMaterial)
                float4 _Normales_ST;
                float4 _CouleurEau;
                float4 _CarrelageA;
                float4 _CarrelageB;
                float4 _VitesseA;
                float4 _VitesseB;
                float _ForceNormales;
                float _Rugosite;
            CBUFFER_END

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
                float2 uvA = IN.uv * _CarrelageA.xy + _VitesseA.xy * _Time.y;
                float2 uvB = IN.uv * _CarrelageB.xy + _VitesseB.xy * _Time.y;

                float3 a = UnpackNormal(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, uvA));
                float3 b = UnpackNormal(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, uvB));

                float3 normalTS = normalize(float3(a.xy + b.xy, a.z * b.z));
                normalTS.xy *= _ForceNormales;
                normalTS = normalize(normalTS);

                float3x3 versMonde = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS));

                InputData donnees = (InputData)0;
                donnees.positionWS = IN.positionWS;
                donnees.normalWS = normalize(mul(normalTS, versMonde));
                donnees.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                donnees.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                donnees.bakedGI = SampleSH(donnees.normalWS);
                donnees.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                donnees.shadowMask = half4(1, 1, 1, 1);
                donnees.fogCoord = IN.fogCoord;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = _CouleurEau.rgb;
                surface.metallic = 0.0;
                surface.specular = half3(0, 0, 0);
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
