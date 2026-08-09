Shader "Cours/07_Hologramme2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Couleur ("Couleur", Color) = (0.25, 0.85, 1.0, 1.0)
        _OpaciteBase ("Opacite de base", Range(0, 1)) = 0.65
        _DensiteLignes ("Densite des lignes", Range(1, 400)) = 90.0
        _VitesseLignes ("Vitesse des lignes", Range(-4, 4)) = -0.6
        _ForceLignes ("Force des lignes", Range(0, 1)) = 0.55
        _VitesseBalayage ("Vitesse du balayage", Range(0, 2)) = 0.35
        _ForceBalayage ("Force du balayage", Range(0, 3)) = 1.2
        _ForceGlitch ("Force du glitch", Range(0, 0.2)) = 0.02
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
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Couleur;
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
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.color = IN.color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            float bande = floor(IN.uv.y * _HauteurBandes);
            float pas = floor(_Time.y * _FrequenceGlitch);
            float actif = step(0.93, Bruit1(bande + pas * 13.37));
            float decalage = actif * _ForceGlitch * (Bruit1(bande + pas) * 2.0 - 1.0);
            
            float2 uv = float2(clamp(IN.uv.x + decalage, 0.0, 1.0), IN.uv.y);
            half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            
            float lignes = frac(IN.uv.y * _DensiteLignes + _Time.y * _VitesseLignes);
            lignes = lerp(1.0, smoothstep(0.0, 0.45, lignes), _ForceLignes);
            
            float balayage = pow(frac(IN.uv.y - _Time.y * _VitesseBalayage), 12.0);
            float intensite = (_OpaciteBase + balayage * _ForceBalayage) * lignes;
            
            return half4(_Couleur.rgb * intensite, sprite.a * intensite) * IN.color;
        }
        ENDHLSL

        Pass
        {
            Name "Sprite2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "SpriteForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
