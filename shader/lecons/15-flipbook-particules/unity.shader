Shader "Cours/15_Flipbook"
{
    Properties
    {
        _BaseMap ("Feuille d'images", 2D) = "white" {}
        _Grille ("Grille (colonnes, lignes)", Vector) = (8, 8, 0, 0)
        _ImagesParSeconde ("Images par seconde", Range(0, 120)) = 30
        [Toggle] _MelangerImages ("Melanger les images", Float) = 1
        [HDR] _Teinte ("Teinte", Color) = (1, 1, 1, 1)
        _Intensite ("Intensite", Range(0, 10)) = 2.0
        _DecalageAleatoire ("Decalage par instance", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Flipbook"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Grille;
                float4 _Teinte;
                float _ImagesParSeconde;
                float _MelangerImages;
                float _Intensite;
                float _DecalageAleatoire;
            CBUFFER_END

            float2 UvImage(float2 uv, float indice)
            {
                float total = _Grille.x * _Grille.y;
                float i = fmod(floor(indice), total);
                i += total * step(i, -0.5);

                float colonne = fmod(i, _Grille.x);
                float ligne = _Grille.y - 1.0 - floor(i / _Grille.x);

                return (clamp(uv, 0.001, 0.999) + float2(colonne, ligne)) / _Grille.xy;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float depart = _DecalageAleatoire * _Grille.x * _Grille.y * IN.color.r;
                float curseur = _Time.y * _ImagesParSeconde + depart;

                half4 image = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, UvImage(IN.uv, curseur));

                if (_MelangerImages > 0.5)
                {
                    half4 suivante = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, UvImage(IN.uv, curseur + 1.0));
                    image = lerp(image, suivante, frac(curseur));
                }

                return half4(image.rgb * _Teinte.rgb * _Intensite * image.a, image.a);
            }
            ENDHLSL
        }
    }
}
