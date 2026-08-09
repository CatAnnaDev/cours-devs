Shader "Cours/17_Empreinte"
{
    Properties
    {
        _MainTex ("Precedent", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Empreinte"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAXIMUM_PRESSEURS 16

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Presseurs[MAXIMUM_PRESSEURS];
            int _NombrePresseurs;
            float4 _Zone;
            float _Persistance;
            float _Durete;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float precedent = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r;
                float2 monde = (IN.uv - 0.5) * _Zone.zw + _Zone.xy;

                float ajout = 0.0;
                for (int i = 0; i < MAXIMUM_PRESSEURS; i++)
                {
                    if (i >= _NombrePresseurs)
                    {
                        break;
                    }
                    float rayon = _Presseurs[i].z;
                    float force = _Presseurs[i].w;
                    float distanceCentre = distance(monde, _Presseurs[i].xy);
                    ajout += force * smoothstep(rayon, rayon * _Durete, distanceCentre);
                }

                float resultat = saturate(precedent * _Persistance + ajout);
                return half4(resultat, resultat, resultat, 1.0);
            }
            ENDHLSL
        }
    }
}
