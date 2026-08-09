Shader "Cours/03_LeTemps2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Teinte ("Teinte", Color) = (0.35, 0.80, 1.0, 1.0)
        _Carrelage ("Carrelage", Vector) = (1, 2, 0, 0)
        _Vitesse ("Vitesse UV", Vector) = (0, -0.35, 0, 0)
        _PulsationVitesse ("Pulsation vitesse", Range(0, 12)) = 2.0
        _PulsationForce ("Pulsation force", Range(0, 1)) = 0.35
        _Intensite ("Intensite", Range(0, 10)) = 2.0
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
            float4 _Teinte;
            float4 _Carrelage;
            float4 _Vitesse;
            float _PulsationVitesse;
            float _PulsationForce;
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
            float2 uv = frac(IN.uv * _Carrelage.xy + _Vitesse.xy * _Time.y);
            half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            
            half pulsation = sin(_Time.y * _PulsationVitesse) * 0.5 + 0.5;
            half force = lerp(1.0 - _PulsationForce, 1.0, pulsation);
            
            return half4(sprite.rgb * _Teinte.rgb * _Intensite * force, sprite.a) * IN.color;
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
