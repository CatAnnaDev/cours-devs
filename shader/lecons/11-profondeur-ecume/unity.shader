Shader "Cours/11_ProfondeurEcume"
{
    Properties
    {
        _BruitEcume ("Bruit d'ecume", 2D) = "gray" {}
        _CouleurPeuProfonde ("Couleur peu profonde", Color) = (0.12, 0.58, 0.62, 1.0)
        _CouleurProfonde ("Couleur profonde", Color) = (0.02, 0.10, 0.22, 1.0)
        _CouleurEcume ("Couleur de l'ecume", Color) = (1, 1, 1, 1)
        _ProfondeurMax ("Profondeur max", Range(0.05, 20)) = 3.0
        _LargeurEcume ("Largeur de l'ecume", Range(0.01, 3)) = 0.35
        _OpaciteBord ("Opacite au bord", Range(0, 1)) = 0.15
        _OpaciteFond ("Opacite au fond", Range(0, 1)) = 0.92
        _CarrelageEcume ("Carrelage de l'ecume", Range(0.1, 30)) = 6.0
        _VitesseEcume ("Vitesse de l'ecume", Vector) = (0.02, 0.013, 0, 0)
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
            Name "Eau"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_BruitEcume);
            SAMPLER(sampler_BruitEcume);

            CBUFFER_START(UnityPerMaterial)
                float4 _BruitEcume_ST;
                float4 _CouleurPeuProfonde;
                float4 _CouleurProfonde;
                float4 _CouleurEcume;
                float4 _VitesseEcume;
                float _ProfondeurMax;
                float _LargeurEcume;
                float _OpaciteBord;
                float _OpaciteFond;
                float _CarrelageEcume;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uvEcran = GetNormalizedScreenSpaceUV(IN.positionCS);

                float brut = SampleSceneDepth(uvEcran);
                float profondeurFond = LinearEyeDepth(brut, _ZBufferParams);
                float profondeurSurface = -TransformWorldToView(IN.positionWS).z;

                float epaisseur = max(profondeurFond - profondeurSurface, 0.0);
                float melange = saturate(epaisseur / _ProfondeurMax);

                float2 uvBruit = IN.uv * _CarrelageEcume + _VitesseEcume.xy * _Time.y;
                half grain = SAMPLE_TEXTURE2D(_BruitEcume, sampler_BruitEcume, uvBruit).r;

                float bord = 1.0 - saturate(epaisseur / _LargeurEcume);
                float ecume = smoothstep(grain * 0.6, grain * 0.6 + 0.18, bord);

                half3 couleur = lerp(_CouleurPeuProfonde.rgb, _CouleurProfonde.rgb, melange);
                couleur = lerp(couleur, _CouleurEcume.rgb, ecume);

                half alpha = lerp(lerp(_OpaciteBord, _OpaciteFond, melange), 1.0, ecume);
                return half4(couleur, alpha);
            }
            ENDHLSL
        }
    }
}
