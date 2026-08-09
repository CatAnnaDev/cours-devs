Shader "Cours/18_Bouclier"
{
    Properties
    {
        [HDR] _Couleur ("Couleur", Color) = (0.30, 0.70, 1.0, 1.0)
        [HDR] _CouleurIntersection ("Couleur d'intersection", Color) = (0.85, 0.95, 1.0, 1.0)
        _OpaciteBase ("Opacite de base", Range(0, 1)) = 0.04
        _PuissanceFresnel ("Puissance fresnel", Range(0.5, 16)) = 3.0
        _ForceFresnel ("Force fresnel", Range(0, 4)) = 1.0
        _EchelleHexagones ("Echelle des hexagones", Range(1, 60)) = 14.0
        _EpaisseurHexagones ("Epaisseur des hexagones", Range(0.001, 0.5)) = 0.06
        _ForceHexagones ("Force des hexagones", Range(0, 2)) = 0.35
        _VitesseOnde ("Vitesse de l'onde", Range(0.1, 10)) = 2.5
        _DureeOnde ("Duree de l'onde", Range(0.1, 5)) = 1.2
        _LargeurOnde ("Largeur de l'onde", Range(0.01, 1)) = 0.22
        _ForceOnde ("Force de l'onde", Range(0, 8)) = 3.0
        _LargeurIntersection ("Largeur d'intersection", Range(0.01, 2)) = 0.35
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
            Name "Bouclier"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define MAXIMUM_IMPACTS 8

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
                float3 positionOS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Couleur;
                float4 _CouleurIntersection;
                float _OpaciteBase;
                float _PuissanceFresnel;
                float _ForceFresnel;
                float _EchelleHexagones;
                float _EpaisseurHexagones;
                float _ForceHexagones;
                float _VitesseOnde;
                float _DureeOnde;
                float _LargeurOnde;
                float _ForceOnde;
                float _LargeurIntersection;
            CBUFFER_END

            float4 _Impacts[MAXIMUM_IMPACTS];
            int _NombreImpacts;

            float BordHexagone(float2 p)
            {
                p = abs(p);
                return max(dot(p, normalize(float2(1.0, 1.7320508))), p.x);
            }

            float2 ModuloPositif(float2 x, float2 y)
            {
                return x - floor(x / y) * y;
            }

            float GrilleHexagonale(float2 p)
            {
                float2 maille = float2(1.0, 1.7320508);
                float2 a = ModuloPositif(p, maille) - maille * 0.5;
                float2 b = ModuloPositif(p - maille * 0.5, maille) - maille * 0.5;
                float2 local = lerp(a, b, step(dot(b, b), dot(a, a)));
                return 0.5 - BordHexagone(local);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 vueWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, vueWS)), _PuissanceFresnel) * _ForceFresnel;

                float distanceBord = GrilleHexagonale(IN.uv * _EchelleHexagones);
                float hexagones = smoothstep(_EpaisseurHexagones, 0.0, distanceBord) * _ForceHexagones;

                float onde = 0.0;
                for (int i = 0; i < MAXIMUM_IMPACTS; i++)
                {
                    if (i >= _NombreImpacts)
                    {
                        break;
                    }
                    float age = _Time.y - _Impacts[i].w;
                    if (age < 0.0 || age > _DureeOnde)
                    {
                        continue;
                    }
                    float rayon = age * _VitesseOnde;
                    float ecart = abs(distance(IN.positionOS, _Impacts[i].xyz) - rayon);
                    onde += smoothstep(_LargeurOnde, 0.0, ecart) * (1.0 - age / _DureeOnde);
                }
                onde *= _ForceOnde;

                float2 uvEcran = GetNormalizedScreenSpaceUV(IN.positionCS);
                float profondeurScene = LinearEyeDepth(SampleSceneDepth(uvEcran), _ZBufferParams);
                float profondeurPixel = -TransformWorldToView(IN.positionWS).z;
                float epaisseur = max(profondeurScene - profondeurPixel, 0.0);
                float intersection = 1.0 - saturate(epaisseur / _LargeurIntersection);

                float intensite = _OpaciteBase + fresnel + hexagones + onde;

                half3 couleur = _Couleur.rgb * intensite + _CouleurIntersection.rgb * intersection;
                return half4(couleur, saturate(intensite + intersection));
            }
            ENDHLSL
        }
    }
}
