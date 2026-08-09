Shader "Cours/06_Fresnel"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        [HDR] _CouleurContour ("Couleur du contour", Color) = (0.35, 0.75, 1.0, 1.0)
        _Puissance ("Puissance", Range(0.5, 16)) = 4.0
        _Intensite ("Intensite", Range(0, 10)) = 2.5
        _SeuilBas ("Seuil bas", Range(0, 1)) = 0.0
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
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _CouleurContour;
                float _Puissance;
                float _Intensite;
                float _SeuilBas;
            CBUFFER_END

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

                float face = saturate(dot(normalWS, vueWS));
                half fresnel = pow(1.0 - face, _Puissance);
                fresnel = smoothstep(_SeuilBas, 1.0, fresnel);

                half3 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                return half4(base + _CouleurContour.rgb * fresnel * _Intensite, 1.0);
            }
            ENDHLSL
        }
    }
}
