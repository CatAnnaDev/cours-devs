Shader "Cours/16_Decalcomanie"
{
    Properties
    {
        _BaseMap ("Texture du decal", 2D) = "white" {}
        _Teinte ("Teinte", Color) = (1, 1, 1, 1)
        _Opacite ("Opacite", Range(0, 1)) = 1.0
        _AngleMax ("Angle maximum (degres)", Range(0, 89)) = 60.0
        _AdoucissementBord ("Adoucissement du bord", Range(0, 0.49)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Decalcomanie"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Teinte;
                float _Opacite;
                float _AngleMax;
                float _AdoucissementBord;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uvEcran = GetNormalizedScreenSpaceUV(IN.positionCS);

                float brut = SampleSceneDepth(uvEcran);
                float3 positionWS = ComputeWorldSpacePosition(uvEcran, brut, UNITY_MATRIX_I_VP);
                float3 normaleScene = normalize(cross(ddx(positionWS), ddy(positionWS)));

                float3 positionOS = TransformWorldToObject(positionWS);
                float3 distanceBord = abs(positionOS);

                clip(0.5 - max(max(distanceBord.x, distanceBord.y), distanceBord.z));

                float3 axe = normalize(TransformObjectToWorldDir(float3(0, 1, 0)));
                clip(abs(dot(normaleScene, axe)) - cos(radians(_AngleMax)));

                half4 echantillon = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, positionOS.xz + 0.5);

                float3 marge = smoothstep(0.5, 0.5 - _AdoucissementBord - 0.001, distanceBord);
                float bord = marge.x * marge.y * marge.z;

                return half4(echantillon.rgb * _Teinte.rgb, echantillon.a * _Opacite * bord);
            }
            ENDHLSL
        }
    }
}
