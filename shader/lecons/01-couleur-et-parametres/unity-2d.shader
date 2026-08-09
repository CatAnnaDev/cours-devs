Shader "Cours/01_Neon2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Couleur ("Couleur", Color) = (0.10, 0.70, 1.0, 1.0)
        _Intensite ("Intensite", Range(0, 20)) = 4.0
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
            float _Intensite;
        CBUFFER_END

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
            half3 couleur = sprite.rgb * _Couleur.rgb * _Intensite;
            return half4(couleur, sprite.a) * IN.color;
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
