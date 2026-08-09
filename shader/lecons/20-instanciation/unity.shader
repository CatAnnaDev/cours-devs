Shader "Cours/20_Instanciation"
{
    Properties
    {
        _BaseMap ("Texture de base", 2D) = "white" {}
        _CouleurJeune ("Couleur jeune", Color) = (0.35, 0.70, 0.30, 1)
        _CouleurSeche ("Couleur seche", Color) = (0.65, 0.58, 0.25, 1)
        _VariationEchelle ("Variation d'echelle", Range(0, 1)) = 0.35
        _DirectionVent ("Direction du vent", Vector) = (1, 0.3, 0, 0)
        _ForceVent ("Force du vent", Range(0, 1)) = 0.12
        _FrequenceVent ("Frequence du vent", Range(0, 4)) = 1.4
        _LongueurRafale ("Longueur de rafale", Range(0.5, 40)) = 6.0
        _SeuilAlpha ("Seuil alpha", Range(0, 1)) = 0.5
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

        Pass
        {
            Name "Herbe"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 teinte : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _CouleurJeune;
                float4 _CouleurSeche;
                float4 _DirectionVent;
                float _VariationEchelle;
                float _ForceVent;
                float _FrequenceVent;
                float _LongueurRafale;
                float _SeuilAlpha;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Variation)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Variation)
            UNITY_INSTANCING_BUFFER_END(Variation)

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float4 variation = UNITY_ACCESS_INSTANCED_PROP(Variation, _Variation);
                float graine = variation.x;
                float maturite = variation.y;
                float grandeur = variation.z;

                OUT.teinte = lerp(_CouleurJeune.rgb, _CouleurSeche.rgb, maturite);

                float3 positionOS = IN.positionOS.xyz;
                positionOS *= lerp(1.0 - _VariationEchelle, 1.0 + _VariationEchelle, grandeur);

                float3 positionWS = TransformObjectToWorld(positionOS);
                float2 direction = normalize(_DirectionVent.xy);
                float phase = dot(positionWS.xz, direction) / _LongueurRafale + graine * 6.28318530718;

                float souplesse = saturate(positionOS.y) * IN.color.r;
                float rafale = sin(_Time.y * _FrequenceVent - phase);
                positionWS.xz += direction * rafale * _ForceVent * souplesse;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 echantillon = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(echantillon.a - _SeuilAlpha);

                return half4(echantillon.rgb * IN.teinte, 1.0);
            }
            ENDHLSL
        }
    }
}
