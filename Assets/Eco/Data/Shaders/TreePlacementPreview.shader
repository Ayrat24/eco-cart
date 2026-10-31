Shader "Eco/TreePlacementPreview"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (0.15, 1, 0.4, 0.4)
        _VertexColors("Vertex Colors", Float) = 0
        _GroundOverlay("Ground Overlay", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth Test", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            ZTest [_ZTest]
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                float _VertexColors;
                float _GroundOverlay;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; half shade : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(EcoBendWorldPosition(world, _EcoWorldBend));
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = lerp(half4(1,1,1,1), input.color, _VertexColors);
                output.shade = _GroundOverlay > 0.5 ? 1 : 0.65 + 0.35 * abs(TransformObjectToWorldNormal(input.normalOS).y);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(alpha - 0.3);
                return half4(_Tint.rgb * input.shade * input.color.rgb, _Tint.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
