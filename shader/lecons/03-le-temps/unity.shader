Shader "Cours/03_LeTemps"
{
    Properties
    {
        _Motif ("Motif", 2D) = "white" {}
        [HDR] _Teinte ("Teinte", Color) = (0.35, 0.80, 1.0, 1.0)
        _Vitesse ("Vitesse UV", Vector) = (0, -0.35, 0, 0)
        _PulsationVitesse ("Pulsation vitesse", Range(0, 12)) = 2.0
        _PulsationForce ("Pulsation force", Range(0, 1)) = 0.35
        _EmissionForce ("Emission force", Range(0, 10)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off

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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_Motif);
            SAMPLER(sampler_Motif);

            CBUFFER_START(UnityPerMaterial)
                float4 _Motif_ST;
                float4 _Teinte;
                float4 _Vitesse;
                float _PulsationVitesse;
                float _PulsationForce;
                float _EmissionForce;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _Motif);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv + _Vitesse.xy * _Time.y;
                half bande = SAMPLE_TEXTURE2D(_Motif, sampler_Motif, uv).r;

                half pulsation = sin(_Time.y * _PulsationVitesse) * 0.5 + 0.5;
                half intensite = lerp(1.0 - _PulsationForce, 1.0, pulsation);

                return half4(_Teinte.rgb * bande * _EmissionForce * intensite, 1.0);
            }
            ENDHLSL
        }
    }
}
