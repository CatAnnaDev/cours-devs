Shader "Cours/17_NeigeEcrasee"
{
    Properties
    {
        _Deformation ("Texture de deformation", 2D) = "black" {}
        _Zone ("Zone (centre xz, taille xz)", Vector) = (0, 0, 20, 20)
        _ProfondeurMax ("Profondeur maximum", Range(0, 2)) = 0.25
        _CouleurSurface ("Couleur de surface", Color) = (0.92, 0.94, 1.0, 1)
        _CouleurCreux ("Couleur du creux", Color) = (0.58, 0.64, 0.80, 1)
        _CouleurBourrelet ("Couleur du bourrelet", Color) = (1, 1, 1, 1)
        _LargeurBourrelet ("Largeur du bourrelet", Range(0.001, 1)) = 0.25
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
            Name "Neige"
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogCoord : TEXCOORD1;
            };

            TEXTURE2D(_Deformation);
            SAMPLER(sampler_Deformation);

            CBUFFER_START(UnityPerMaterial)
                float4 _Deformation_ST;
                float4 _Deformation_TexelSize;
                float4 _Zone;
                float4 _CouleurSurface;
                float4 _CouleurCreux;
                float4 _CouleurBourrelet;
                float _ProfondeurMax;
                float _LargeurBourrelet;
            CBUFFER_END

            float2 UvZone(float3 monde)
            {
                return (monde.xz - _Zone.xy) / _Zone.zw + 0.5;
            }

            float Deformation(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_Deformation, sampler_Deformation, uv, 0).r;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS.y -= Deformation(UvZone(positionWS)) * _ProfondeurMax;

                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = UvZone(IN.positionWS);
                float2 pas = _Deformation_TexelSize.xy;

                float centre = Deformation(uv);
                float dx = Deformation(uv + float2(pas.x, 0)) - Deformation(uv - float2(pas.x, 0));
                float dz = Deformation(uv + float2(0, pas.y)) - Deformation(uv - float2(0, pas.y));

                float tailleTexelX = _Zone.z * pas.x;
                float tailleTexelZ = _Zone.w * pas.y;
                float3 normaleWS = normalize(float3(
                    dx * _ProfondeurMax / (2.0 * tailleTexelX),
                    1.0,
                    dz * _ProfondeurMax / (2.0 * tailleTexelZ)));

                float pente = length(float2(dx, dz));
                float bourrelet = smoothstep(0.0, _LargeurBourrelet, pente) * (1.0 - centre);

                half3 couleur = lerp(_CouleurSurface.rgb, _CouleurCreux.rgb, centre);
                couleur = lerp(couleur, _CouleurBourrelet.rgb, bourrelet);

                InputData donnees = (InputData)0;
                donnees.positionWS = IN.positionWS;
                donnees.normalWS = normaleWS;
                donnees.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                donnees.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                donnees.bakedGI = SampleSH(normaleWS);
                donnees.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                donnees.shadowMask = half4(1, 1, 1, 1);
                donnees.fogCoord = IN.fogCoord;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = couleur;
                surface.metallic = 0.0;
                surface.smoothness = 1.0 - lerp(0.45, 0.85, centre);
                surface.occlusion = 1.0;
                surface.alpha = 1.0;

                half4 sortie = UniversalFragmentPBR(donnees, surface);
                sortie.rgb = MixFog(sortie.rgb, IN.fogCoord);
                return sortie;
            }
            ENDHLSL
        }
    }
}
