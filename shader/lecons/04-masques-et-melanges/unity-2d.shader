Shader "Cours/04_MasquesEtMelanges2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Usure ("Texture d usure", 2D) = "white" {}
        _Masque ("Masque", 2D) = "gray" {}
        _Couverture ("Couverture", Range(0, 1)) = 0.5
        _Nettete ("Nettete", Range(0.002, 0.5)) = 0.12
        _Irregularite ("Irregularite", Range(0, 1)) = 0.35
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
        TEXTURE2D(_Usure);
        SAMPLER(sampler_Usure);
        TEXTURE2D(_Masque);
        SAMPLER(sampler_Masque);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Usure_ST;
            float4 _Masque_ST;
            float _Couverture;
            float _Nettete;
            float _Irregularite;
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
            half4 usure = SAMPLE_TEXTURE2D(_Usure, sampler_Usure, IN.uv);
            
            half grain = SAMPLE_TEXTURE2D(_Masque, sampler_Masque, IN.uv).r;
            float valeur = IN.uv.y - _Irregularite * (1.0 - grain);
            
            float seuil = 1.0 - _Couverture;
            float melange = smoothstep(seuil - _Nettete, seuil + _Nettete, valeur);
            
            return half4(lerp(base.rgb, usure.rgb, melange), base.a) * IN.color;
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
