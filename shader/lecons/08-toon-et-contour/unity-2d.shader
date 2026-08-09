Shader "Cours/08_Posterisation2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Palette ("Palette (rampe 1D)", 2D) = "white" {}
        _Niveaux ("Niveaux", Range(2, 32)) = 5
        [Toggle] _UtiliserPalette ("Utiliser la palette", Float) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1.15
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
        TEXTURE2D(_Palette);
        SAMPLER(sampler_Palette);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Palette_ST;
            float _Niveaux;
            float _UtiliserPalette;
            float _Saturation;
        CBUFFER_END

        half Luminance2D(half3 couleur)
        {
            return dot(couleur, half3(0.2126, 0.7152, 0.0722));
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
            
            half3 quantifiee = floor(sprite.rgb * _Niveaux + 0.5) / _Niveaux;
            half gris = Luminance2D(quantifiee);
            quantifiee = lerp(half3(gris, gris, gris), quantifiee, _Saturation);
            
            float indice = clamp(Luminance2D(sprite.rgb), 0.01, 0.99);
            half3 depuis_palette = SAMPLE_TEXTURE2D(_Palette, sampler_Palette, float2(indice, 0.5)).rgb;
            
            half3 finale = lerp(quantifiee, depuis_palette, _UtiliserPalette);
            return half4(saturate(finale), sprite.a) * IN.color;
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
