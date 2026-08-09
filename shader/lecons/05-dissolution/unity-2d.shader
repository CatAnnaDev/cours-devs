Shader "Cours/05_Dissolution2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Bruit ("Bruit", 2D) = "gray" {}
        _Progression ("Progression", Range(0, 1)) = 0.0
        _EchelleBruit ("Echelle du bruit", Range(0.5, 16)) = 4.0
        [HDR] _CouleurBord ("Couleur du bord", Color) = (1.0, 0.35, 0.05, 1.0)
        _LargeurBord ("Largeur du bord", Range(0.001, 0.4)) = 0.08
        _IntensiteBord ("Intensite du bord", Range(0, 20)) = 6.0
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
        TEXTURE2D(_Bruit);
        SAMPLER(sampler_Bruit);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _Bruit_ST;
            float4 _CouleurBord;
            float _Progression;
            float _EchelleBruit;
            float _LargeurBord;
            float _IntensiteBord;
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
            half grain = SAMPLE_TEXTURE2D(_Bruit, sampler_Bruit, IN.uv * _EchelleBruit).r;
            
            float seuil = lerp(-_LargeurBord, 1.0 + _LargeurBord, _Progression);
            float visible = grain - seuil;
            
            float presence = step(0.0, visible);
            float bord = (1.0 - smoothstep(0.0, _LargeurBord, visible)) * presence;
            
            half3 couleur = sprite.rgb + _CouleurBord.rgb * bord * _IntensiteBord;
            return half4(couleur, sprite.a * presence) * IN.color;
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
