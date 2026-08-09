Shader "Cours/12_VerreEtChaleur"
{
    Properties
    {
        [Normal] _Normales ("Normal map", 2D) = "bump" {}
        _Teinte ("Teinte", Color) = (0.86, 0.94, 1.0, 1.0)
        [HDR] _CouleurBord ("Couleur du bord", Color) = (1, 1, 1, 1)
        _ForceRefraction ("Force de refraction", Range(0, 0.2)) = 0.03
        _Flou ("Flou", Range(0, 5)) = 0.0
        _Carrelage ("Carrelage", Range(0.1, 20)) = 2.0
        _Vitesse ("Vitesse", Vector) = (0.03, 0.05, 0, 0)
        _PuissanceFresnel ("Puissance fresnel", Range(0.5, 16)) = 4.0
        _ForceBord ("Force du bord", Range(0, 2)) = 0.35
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
            Name "Verre"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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

            TEXTURE2D(_Normales);
            SAMPLER(sampler_Normales);

            CBUFFER_START(UnityPerMaterial)
                float4 _Normales_ST;
                float4 _Teinte;
                float4 _CouleurBord;
                float4 _Vitesse;
                float _ForceRefraction;
                float _Flou;
                float _Carrelage;
                float _PuissanceFresnel;
                float _ForceBord;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uvBruit = IN.uv * _Carrelage + _Vitesse.xy * _Time.y;
                float3 perturbation = UnpackNormal(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, uvBruit));

                float2 uvEcran = GetNormalizedScreenSpaceUV(IN.positionCS);
                uvEcran = clamp(uvEcran + perturbation.xy * _ForceRefraction, 0.001, 0.999);

                half3 derriere = SAMPLE_TEXTURE2D_X_LOD(
                    _CameraOpaqueTexture,
                    sampler_CameraOpaqueTexture,
                    uvEcran,
                    _Flou).rgb;

                float3 normalWS = normalize(IN.normalWS);
                float3 vueWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, vueWS)), _PuissanceFresnel);

                half3 couleur = derriere * _Teinte.rgb + _CouleurBord.rgb * fresnel * _ForceBord;
                return half4(couleur, 1.0);
            }
            ENDHLSL
        }
    }
}
