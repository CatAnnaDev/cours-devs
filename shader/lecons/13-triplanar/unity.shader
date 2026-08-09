Shader "Cours/13_Triplanar"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        [Normal] _Normales ("Normal map", 2D) = "bump" {}
        _Echelle ("Echelle", Range(0.01, 4)) = 0.25
        _Nettete ("Nettete du melange", Range(1, 16)) = 4.0
        _ForceNormales ("Force des normales", Range(0, 3)) = 1.0
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
            Name "Triplanar"
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Normales);
            SAMPLER(sampler_Normales);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Normales_ST;
                float _Echelle;
                float _Nettete;
                float _ForceNormales;
                float _Rugosite;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normale = normalize(IN.normalWS);
                float3 poids = pow(abs(normale), _Nettete);
                poids /= (poids.x + poids.y + poids.z);

                float3 p = IN.positionWS * _Echelle;

                half3 couleurX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.zy).rgb;
                half3 couleurY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xz).rgb;
                half3 couleurZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xy).rgb;

                float3 normaleX = UnpackNormal(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, p.zy));
                float3 normaleY = UnpackNormal(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, p.xz));
                float3 normaleZ = UnpackNormal(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, p.xy));

                normaleX.xy *= _ForceNormales;
                normaleY.xy *= _ForceNormales;
                normaleZ.xy *= _ForceNormales;

                normaleX = float3(normaleX.xy + normale.zy, abs(normaleX.z) * normale.x);
                normaleY = float3(normaleY.xy + normale.xz, abs(normaleY.z) * normale.y);
                normaleZ = float3(normaleZ.xy + normale.xy, abs(normaleZ.z) * normale.z);

                float3 assemblee = normalize(
                    normaleX.zyx * poids.x +
                    normaleY.xzy * poids.y +
                    normaleZ.xyz * poids.z);

                InputData donnees = (InputData)0;
                donnees.positionWS = IN.positionWS;
                donnees.normalWS = assemblee;
                donnees.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                donnees.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                donnees.bakedGI = SampleSH(assemblee);
                donnees.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                donnees.shadowMask = half4(1, 1, 1, 1);
                donnees.fogCoord = IN.fogCoord;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = couleurX * poids.x + couleurY * poids.y + couleurZ * poids.z;
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
