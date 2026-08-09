Shader "Cours/02_TextureEtUV2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Carrelee ("Texture carrelee (hors atlas)", 2D) = "white" {}
        _Teinte ("Teinte", Color) = (1, 1, 1, 1)
        _Carrelage ("Carrelage", Vector) = (4, 4, 0, 0)
        _Decalage ("Decalage", Vector) = (0, 0, 0, 0)
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
        TEXTURE2D(_Carrelee);
        SAMPLER(sampler_Carrelee);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Carrelee_ST;
            float4 _Teinte;
            float4 _Carrelage;
            float4 _Decalage;
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
            float2 uv = IN.uv * _Carrelage.xy + _Decalage.xy;
            half4 echantillon = SAMPLE_TEXTURE2D(_Carrelee, sampler_Carrelee, uv);
            return half4(echantillon.rgb * _Teinte.rgb, echantillon.a) * IN.color;
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
