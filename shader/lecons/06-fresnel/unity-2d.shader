Shader "Cours/06_Contour2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _CouleurContour ("Couleur du contour", Color) = (1.0, 0.95, 0.35, 1.0)
        _Epaisseur ("Epaisseur en pixels", Range(0, 8)) = 1.5
        _SeuilAlpha ("Seuil alpha", Range(0, 1)) = 0.1
        [Toggle] _ContourSeul ("Contour seul", Float) = 0
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
            float4 _CouleurContour;
            float _Epaisseur;
            float _SeuilAlpha;
            float _ContourSeul;
        CBUFFER_END

        half AlphaEn(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
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
            half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            float2 pas = _MainTex_TexelSize.xy * _Epaisseur;
            
            half voisins = 0.0;
            voisins = max(voisins, AlphaEn(IN.uv + float2(pas.x, 0.0)));
            voisins = max(voisins, AlphaEn(IN.uv - float2(pas.x, 0.0)));
            voisins = max(voisins, AlphaEn(IN.uv + float2(0.0, pas.y)));
            voisins = max(voisins, AlphaEn(IN.uv - float2(0.0, pas.y)));
            voisins = max(voisins, AlphaEn(IN.uv + pas * 0.70710678));
            voisins = max(voisins, AlphaEn(IN.uv - pas * 0.70710678));
            voisins = max(voisins, AlphaEn(IN.uv + float2(pas.x, -pas.y) * 0.70710678));
            voisins = max(voisins, AlphaEn(IN.uv + float2(-pas.x, pas.y) * 0.70710678));
            
            float dedans = step(_SeuilAlpha, sprite.a);
            float contour = step(_SeuilAlpha, voisins) * (1.0 - dedans);
            
            half3 couleur = lerp(sprite.rgb, _CouleurContour.rgb, contour);
            half alpha = max(sprite.a, contour);
            
            couleur = lerp(couleur, _CouleurContour.rgb, _ContourSeul);
            alpha = lerp(alpha, contour, _ContourSeul);
            
            return half4(couleur, alpha) * IN.color;
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
