Shader "Cours/21_PeintureDeSommets2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _CoucheSecondaire ("Couche secondaire", 2D) = "white" {}
        _Carrelage ("Carrelage", Vector) = (1, 1, 0, 0)
        _Nettete ("Nettete", Range(0.001, 0.5)) = 0.15
        _DecalageSeuil ("Decalage du seuil", Range(-0.5, 0.5)) = 0.0
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
        TEXTURE2D(_CoucheSecondaire);
        SAMPLER(sampler_CoucheSecondaire);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _CoucheSecondaire_ST;
            float4 _Carrelage;
            float _Nettete;
            float _DecalageSeuil;
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
            half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            half4 secondaire = SAMPLE_TEXTURE2D(_CoucheSecondaire, sampler_CoucheSecondaire,
                                                IN.uv * _Carrelage.xy);

            float peinture = IN.color.r;
            float seuil = 0.5 + _DecalageSeuil;
            float melange = smoothstep(seuil - _Nettete, seuil + _Nettete, peinture);

            half3 couleur = lerp(base.rgb, secondaire.rgb, melange);
            return half4(couleur, base.a * IN.color.a);
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
