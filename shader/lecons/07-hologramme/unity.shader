Shader "Cours/07_Hologramme"
{
    Properties
    {
        [HDR] _Couleur ("Couleur", Color) = (0.25, 0.85, 1.0, 1.0)
        _PuissanceFresnel ("Puissance fresnel", Range(0.5, 16)) = 3.0
        _OpaciteBase ("Opacite de base", Range(0, 1)) = 0.10
        _DensiteLignes ("Densite des lignes", Range(1, 400)) = 120.0
        _VitesseLignes ("Vitesse des lignes", Range(-4, 4)) = -0.6
        _ForceLignes ("Force des lignes", Range(0, 1)) = 0.55
        _VitesseBalayage ("Vitesse du balayage", Range(0, 2)) = 0.35
        _ForceBalayage ("Force du balayage", Range(0, 3)) = 1.2
        _ForceGlitch ("Force du glitch", Range(0, 0.3)) = 0.04
        _FrequenceGlitch ("Frequence du glitch", Range(0, 30)) = 8.0
        _HauteurBandes ("Hauteur des bandes", Range(1, 60)) = 14.0
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
            Name "Hologramme"
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
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Couleur;
                float _PuissanceFresnel;
                float _OpaciteBase;
                float _DensiteLignes;
                float _VitesseLignes;
                float _ForceLignes;
                float _VitesseBalayage;
                float _ForceBalayage;
                float _ForceGlitch;
                float _FrequenceGlitch;
                float _HauteurBandes;
            CBUFFER_END

            float Bruit1(float x)
            {
                return frac(sin(x * 91.7) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionOS = IN.positionOS.xyz;
                float bande = floor(positionOS.y * _HauteurBandes);
                float pas = floor(_Time.y * _FrequenceGlitch);
                float actif = step(0.93, Bruit1(bande + pas * 13.37));
                positionOS.x += actif * _ForceGlitch * (Bruit1(bande + pas) * 2.0 - 1.0);

                VertexPositionInputs positions = GetVertexPositionInputs(positionOS);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 vueWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                float face = saturate(dot(normalWS, vueWS));
                float fresnel = pow(1.0 - face, _PuissanceFresnel);

                float lignes = frac(IN.positionOS.y * _DensiteLignes + _Time.y * _VitesseLignes);
                lignes = lerp(1.0, smoothstep(0.0, 0.45, lignes), _ForceLignes);

                float balayage = pow(frac(IN.positionOS.y * 0.35 - _Time.y * _VitesseBalayage), 12.0);

                float intensite = (_OpaciteBase + fresnel + balayage * _ForceBalayage) * lignes;

                return half4(_Couleur.rgb * intensite, saturate(intensite));
            }
            ENDHLSL
        }
    }
}
