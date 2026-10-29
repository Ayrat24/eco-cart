// Generated from the installed Bitgem water graph by CurvedWaterSetup.
Shader "Eco/Curved World/Water"
{
Properties
{
Color_F01C36BF("_ShallowColor", Color) = (0.1019608, 0.4627451, 0.5764706, 1)
Color_7D9A58EC("_DeepColor", Color) = (0.1403524, 0.1653288, 0.3584906, 0)
Vector1_47683D42("_Glossiness", Range(0, 1)) = 0.85
Vector1_3D886DA1("_Metallic", Range(0, 1)) = 0
[Normal][NoScaleOffset]Texture2D_6490A223("_NormalMap", 2D) = "bump" {}
Vector2_37B21477("_UVScale", Vector, 2) = (1, 1, 0, 0)
Vector1_F38B44AA("_DetailScale", Float) = 0.2
Vector1_46E42935("_DetailStrength", Range(0, 1)) = 0.2
Vector1_244B0600("_ScrollSpeed", Float) = 1
_WaveFrequency("_WaveFrequency", Float) = 3
_WaveScale("_WaveScale", Float) = 0.1
_WaveSpeed("_WaveSpeed", Float) = 1
Vector1_B9F56378("_BumpStrength", Float) = 0.5
Vector1_A6A0BC26("_RefractStrength", Float) = 0.08
Vector1_36E8227("_FoamWidth", Float) = 0.5
Vector1_C360A163("_FoamNoise", Float) = 2
Vector1_17E53C12("_DepthScale", Float) = 0.75
Vector1_A0EAD698("_DepthPower", Float) = 1
Vector1_E5C51606("_DepthFoam", Float) = 0
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
}
SubShader
{
Tags
{
"RenderPipeline"="UniversalPipeline"
"RenderType"="Opaque"
"UniversalMaterialType" = "Lit"
"Queue"="Geometry"
"DisableBatching"="False"
"ShaderGraphShader"="true"
"ShaderGraphTargetId"="UniversalLitSubTarget"
}
Pass
{
    Name "ForwardLit"
    Tags
    {
        "LightMode" = "UniversalForward"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_COLOR
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD
#define REQUIRE_DEPTH_TEXTURE
#define REQUIRE_OPAQUE_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
float3 ecoMaterialNormalWS;
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
 float4 color;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
float3 ecoMaterialNormalWS : INTERP11;
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 color : INTERP7;
 float4 fogFactorAndVertexLight : INTERP8;
 float3 positionWS : INTERP9;
 float3 normalWS : INTERP10;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Preview_float2(float2 In, out float2 Out)
{
    Out = In;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

void Unity_Preview_float4(float4 In, out float4 Out)
{
    Out = In;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// unity-custom-func-begin
void NormalLerp_float(float3 InA, float3 InB, float Ratio, out float3 Out){
float3 blend = normalize(float3(InA.rg + InB.rg, InA.b * InB.b));

Out = lerp(lerp(InA, blend, saturate(Ratio * 2)), InB, saturate((Ratio - 0.5) * 2));
}
// unity-custom-func-end

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_SceneColor_float(float4 UV, out float3 Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Preview_float(float In, out float Out)
{
    Out = In;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 ecoMaterialNormalWS;
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.ecoMaterialNormalWS = TransformObjectToWorldNormal(IN.ObjectSpaceNormal);
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4 = Color_7D9A58EC;
float _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float);
float4 _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4 = IN.ScreenPosition;
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_R_1_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[0];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_G_2_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[1];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_B_3_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[2];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[3];
float _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float;
Unity_Subtract_float(_SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float, _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float);
float _Split_83ebd8127a60c68790c7ccd13d68ea95_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_A_4_Float = 0;
float _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float;
Unity_Saturate_float(_Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float, _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float);
float _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float, _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float);
float _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 0.85, _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float);
float _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float;
Unity_Saturate_float(_Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float, _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float);
float _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float);
float _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float, _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float);
float _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 1, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float);
float _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float;
Unity_Add_float(_Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float, _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float);
float _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float = Vector1_17E53C12;
float _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float;
Unity_Multiply_float_float(_Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float, _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float, _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float);
float _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float = Vector1_A0EAD698;
float _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float;
Unity_Power_float(_Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float, _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float, _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float);
float _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float;
Unity_Multiply_float_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, 0.65, _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float);
float _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float;
Unity_Saturate_float(_Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float, _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float);
float4 _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4;
Unity_Lerp_float4(_Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4, _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4, (_Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float.xxxx), _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4);
float _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float = Vector1_36E8227;
float _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float;
Unity_Subtract_float(float(1), _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float, _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float);
float _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float;
Unity_Multiply_float_float(_Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float, 10, _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float);
float _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float = Vector1_E5C51606;
float _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float;
Unity_Divide_float(float(1), _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float);
float _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float, _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float);
float _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float, _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float);
float _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float;
Unity_Saturate_float(_Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float, _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float);
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float = IN.VertexColor[0];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_G_2_Float = IN.VertexColor[1];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_B_3_Float = IN.VertexColor[2];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_A_4_Float = IN.VertexColor[3];
float _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float;
Unity_Maximum_float(_Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float, _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float, _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float);
float _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float;
Unity_Multiply_float_float(_Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float, 2, _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float);
float _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float;
Unity_Power_float(_Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float, float(2), _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float);
float _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float;
Unity_Multiply_float_float(_Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float, 1.5, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float);
float _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float;
Unity_Subtract_float(_Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float, _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float);
UnityTexture2D _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Split_0aebe3f40460e5899da0f9738775cd80_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_0aebe3f40460e5899da0f9738775cd80_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_0aebe3f40460e5899da0f9738775cd80_A_4_Float = 0;
float _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float;
Unity_Multiply_float_float(_Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float, 0.5, _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float);
float _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float;
Unity_Add_float(float(1), _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float, _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float);
float4 _UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4 = IN.uv0;
float2 _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2 = Vector2_37B21477;
float2 _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2;
Unity_Multiply_float2_float2((_UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4.xy), _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2, _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2);
float2 _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2;
Unity_Multiply_float2_float2((_Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float.xx), _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2, _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2);
float2 _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2;
Unity_Preview_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2);
float2 _Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
float _Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float = Vector1_244B0600;
float2 _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2, (_Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float.xx), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2);
float2 _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2;
Unity_Multiply_float2_float2(float2(-0.1, 0.035), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2);
float2 _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2;
Unity_Add_float2(_Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2, _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2);
float4 _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.tex, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.samplerstate, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.GetTransformedUV(_Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2) );
if (_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4);
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.r;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.g;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_B_6_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.b;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_A_7_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.a;
UnityTexture2D _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, float2(-0.01, 0.05), _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2);
float _Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float = Vector1_F38B44AA;
float2 _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, (_Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float.xx), _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2);
float2 _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2;
Unity_Add_float2(_Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2, _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2, _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2);
float4 _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.tex, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.samplerstate, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.GetTransformedUV(_Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2) );
if (_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4);
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.r;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.g;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_B_6_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.b;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_A_7_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.a;
float _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float, _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float);
float _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float);
float _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float;
Unity_Add_float(_Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float, _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float);
float _Property_e95952e8670288869411626e7483404d_Out_0_Float = Vector1_C360A163;
float _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float;
Unity_Multiply_float_float(_Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float, _Property_e95952e8670288869411626e7483404d_Out_0_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float);
float _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float;
Unity_Step_float(_Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float, _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float);
float4 _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4);
float4 _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4;
Unity_Preview_float4(_Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4, _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4);
float4 _Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4 = Color_7D9A58EC;
float _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float;
Unity_Saturate_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float);
float4 _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4;
Unity_Lerp_float4(_Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4, _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4, (_Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float.xxxx), _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4);
float4 _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4);
float _Split_cdf92567be4e0587be8eabeb7b0acefc_R_1_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[0];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_G_2_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[1];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_B_3_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[2];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[3];
float _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float;
Unity_Subtract_float(float(1), _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float, _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float);
float _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float;
Unity_Saturate_float(_Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float, _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float);
float _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float, _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float);
float4 _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4, (_Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float.xxxx), _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4);
float4 _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4;
Unity_Preview_float4(_Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4, _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4);
float _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float = Vector1_46E42935;
float _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float;
Unity_Subtract_float(float(1), _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float, _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float);
float3 _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3;
NormalLerp_float((_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.xyz), (_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.xyz), _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float, _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3);
float _Split_6a5d5302af091b8c93ea83d56bb359d5_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_A_4_Float = 0;
float _Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float;
Unity_Subtract_float(float(1), _Split_6a5d5302af091b8c93ea83d56bb359d5_G_2_Float, _Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float);
float _Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float;
Unity_Multiply_float_float(_Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float, 0.5, _Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float);
float _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float;
Unity_Saturate_float(_Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float, _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float);
float _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float, _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float);
float3 _NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3;
Unity_NormalStrength_float(_NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3, _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float, _NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3);
float _Property_69a99fd328c02180bbfd9849ad10f057_Out_0_Float = Vector1_B9F56378;
float3 _NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3, _Property_69a99fd328c02180bbfd9849ad10f057_Out_0_Float, _NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3);
float _Split_7160bdacb63f1c82919254ec308e3488_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_7160bdacb63f1c82919254ec308e3488_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_7160bdacb63f1c82919254ec308e3488_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_7160bdacb63f1c82919254ec308e3488_A_4_Float = 0;
float _Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float;
Unity_Add_float(_Split_7160bdacb63f1c82919254ec308e3488_G_2_Float, float(0.75), _Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float);
float _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float;
Unity_Saturate_float(_Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float, _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float);
float3 _NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3, _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float, _NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3);
float _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float;
Unity_Subtract_float(float(1), _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float, _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float);
float3 _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3, _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float, _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3);
float4 _ScreenPosition_f74ad8fc29962780a3de5894cfa698c5_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float3 _Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3;
Unity_Preview_float3(_NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3, _Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3);
float3 _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3;
Unity_Normalize_float3(_Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3, _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3);
float _Split_b54baf2c806dd18faa612bd1f586c24a_R_1_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[0];
float _Split_b54baf2c806dd18faa612bd1f586c24a_G_2_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[1];
float _Split_b54baf2c806dd18faa612bd1f586c24a_B_3_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[2];
float _Split_b54baf2c806dd18faa612bd1f586c24a_A_4_Float = 0;
float2 _Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2 = float2(_Split_b54baf2c806dd18faa612bd1f586c24a_R_1_Float, _Split_b54baf2c806dd18faa612bd1f586c24a_G_2_Float);
float _Property_f8c0b1cc7dd32a8f83c31b1ce4a4e920_Out_0_Float = Vector1_A6A0BC26;
float2 _Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2, (_Property_f8c0b1cc7dd32a8f83c31b1ce4a4e920_Out_0_Float.xx), _Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2);
float _Property_a6131faf438a1b8894a3b406edcc5ebd_Out_0_Float = Vector1_17E53C12;
float _Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float;
Unity_Multiply_float_float(_Property_a6131faf438a1b8894a3b406edcc5ebd_Out_0_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float);
float _Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float;
Unity_Power_float(_Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float, float(2), _Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float);
float _Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float;
Unity_Saturate_float(_Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float, _Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float);
float2 _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2, (_Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float.xx), _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2);
float2 _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2, _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2, _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2);
float2 _Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2;
Unity_Add_float2((_ScreenPosition_f74ad8fc29962780a3de5894cfa698c5_Out_0_Vector4.xy), _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2, _Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2);
float3 _SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3;
Unity_SceneColor_float((float4(_Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2, 0.0, 1.0)), _SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3);
float3 _Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3;
Unity_Multiply_float3_float3(_SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3, (_Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float.xxx), _Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3);
float3 _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3;
Unity_Preview_float3(_Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3, _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3);
float _Property_1fc2ad6585a74187a59c88c55fbd590e_Out_0_Float = Vector1_3D886DA1;
float _Multiply_ab99e3f51ec8fc8791570f9004d63f68_Out_2_Float;
Unity_Multiply_float_float(0, 1.5, _Multiply_ab99e3f51ec8fc8791570f9004d63f68_Out_2_Float);
float _Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_ab99e3f51ec8fc8791570f9004d63f68_Out_2_Float, _Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float);
float _Multiply_b268713355bce98ea726cbf48f2b22b2_Out_2_Float;
Unity_Multiply_float_float(_Property_1fc2ad6585a74187a59c88c55fbd590e_Out_0_Float, _Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float, _Multiply_b268713355bce98ea726cbf48f2b22b2_Out_2_Float);
float _Split_6c36aeeb89846088a6f46beb89edeb01_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_6c36aeeb89846088a6f46beb89edeb01_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_6c36aeeb89846088a6f46beb89edeb01_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_6c36aeeb89846088a6f46beb89edeb01_A_4_Float = 0;
float _Add_5a62fb1ac5bef38a85ac2db7e9b9c621_Out_2_Float;
Unity_Add_float(float(0.15), _Split_6c36aeeb89846088a6f46beb89edeb01_G_2_Float, _Add_5a62fb1ac5bef38a85ac2db7e9b9c621_Out_2_Float);
float _Saturate_1aa5822a26860f898e713a38d5944219_Out_1_Float;
Unity_Saturate_float(_Add_5a62fb1ac5bef38a85ac2db7e9b9c621_Out_2_Float, _Saturate_1aa5822a26860f898e713a38d5944219_Out_1_Float);
float _Multiply_168cf0f7b082b086931db23b351f27db_Out_2_Float;
Unity_Multiply_float_float(_Multiply_b268713355bce98ea726cbf48f2b22b2_Out_2_Float, _Saturate_1aa5822a26860f898e713a38d5944219_Out_1_Float, _Multiply_168cf0f7b082b086931db23b351f27db_Out_2_Float);
float _Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float;
Unity_Subtract_float(float(1), _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float, _Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float);
float _Multiply_fb6714a555a32b898efd2b0fe002e34d_Out_2_Float;
Unity_Multiply_float_float(_Multiply_168cf0f7b082b086931db23b351f27db_Out_2_Float, _Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float, _Multiply_fb6714a555a32b898efd2b0fe002e34d_Out_2_Float);
float _Preview_0ead648dd722878590f431f5c578c1e1_Out_1_Float;
Unity_Preview_float(_Multiply_fb6714a555a32b898efd2b0fe002e34d_Out_2_Float, _Preview_0ead648dd722878590f431f5c578c1e1_Out_1_Float);
float _Property_db2c70ae908eaa82b28bc80aad11f92f_Out_0_Float = Vector1_47683D42;
float _Subtract_0b01d3ae1424c98791fc40db5c456faa_Out_2_Float;
Unity_Subtract_float(float(1), _Split_6c36aeeb89846088a6f46beb89edeb01_G_2_Float, _Subtract_0b01d3ae1424c98791fc40db5c456faa_Out_2_Float);
float _Multiply_3f242c97ee180089aaa704614c0403ec_Out_2_Float;
Unity_Multiply_float_float(_Subtract_0b01d3ae1424c98791fc40db5c456faa_Out_2_Float, 0.15, _Multiply_3f242c97ee180089aaa704614c0403ec_Out_2_Float);
float _Add_977a3c2b32c7a68e98d020b8ca06409b_Out_2_Float;
Unity_Add_float(_Multiply_3f242c97ee180089aaa704614c0403ec_Out_2_Float, float(1), _Add_977a3c2b32c7a68e98d020b8ca06409b_Out_2_Float);
float _Multiply_881ca79764d87589a60b678464e04d37_Out_2_Float;
Unity_Multiply_float_float(_Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float, _Add_977a3c2b32c7a68e98d020b8ca06409b_Out_2_Float, _Multiply_881ca79764d87589a60b678464e04d37_Out_2_Float);
float _Multiply_19e292085664df8780de963844026a63_Out_2_Float;
Unity_Multiply_float_float(_Property_db2c70ae908eaa82b28bc80aad11f92f_Out_0_Float, _Multiply_881ca79764d87589a60b678464e04d37_Out_2_Float, _Multiply_19e292085664df8780de963844026a63_Out_2_Float);
float _Multiply_d1803de319716381bd2e252f385bd94c_Out_2_Float;
Unity_Multiply_float_float(_Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float, _Multiply_19e292085664df8780de963844026a63_Out_2_Float, _Multiply_d1803de319716381bd2e252f385bd94c_Out_2_Float);
float _Preview_9efdbaa90673428caf4fa565e36a200e_Out_1_Float;
Unity_Preview_float(_Multiply_d1803de319716381bd2e252f385bd94c_Out_2_Float, _Preview_9efdbaa90673428caf4fa565e36a200e_Out_1_Float);
surface.BaseColor = (_Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4.xyz);
surface.NormalTS = _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3;
surface.Emission = _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3;
surface.Metallic = _Preview_0ead648dd722878590f431f5c578c1e1_Out_1_Float;
surface.Smoothness = _Preview_9efdbaa90673428caf4fa565e36a200e_Out_1_Float;
surface.Occlusion = float(1);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = normalize(input.ecoMaterialNormalWS);      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
    output.VertexColor = input.color;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "GBuffer"
    Tags
    {
        "LightMode" = "UniversalGBuffer"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 4.5
#pragma exclude_renderers gles3 glcore
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
#pragma multi_compile _ REFLECTION_PROBE_ROTATION
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_COLOR
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#define VARYINGS_NEED_SHADOW_COORD
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_GBUFFER
#define _FOG_FRAGMENT 1
#define REQUIRE_DEPTH_TEXTURE
#define REQUIRE_OPAQUE_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
float3 ecoMaterialNormalWS;
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
 float4 color;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
float3 ecoMaterialNormalWS : INTERP11;
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
 float4 color : INTERP7;
 float4 fogFactorAndVertexLight : INTERP8;
 float3 positionWS : INTERP9;
 float3 normalWS : INTERP10;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Preview_float2(float2 In, out float2 Out)
{
    Out = In;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

void Unity_Preview_float4(float4 In, out float4 Out)
{
    Out = In;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// unity-custom-func-begin
void NormalLerp_float(float3 InA, float3 InB, float Ratio, out float3 Out){
float3 blend = normalize(float3(InA.rg + InB.rg, InA.b * InB.b));

Out = lerp(lerp(InA, blend, saturate(Ratio * 2)), InB, saturate((Ratio - 0.5) * 2));
}
// unity-custom-func-end

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_SceneColor_float(float4 UV, out float3 Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Preview_float(float In, out float Out)
{
    Out = In;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 ecoMaterialNormalWS;
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.ecoMaterialNormalWS = TransformObjectToWorldNormal(IN.ObjectSpaceNormal);
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4 = Color_7D9A58EC;
float _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float);
float4 _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4 = IN.ScreenPosition;
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_R_1_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[0];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_G_2_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[1];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_B_3_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[2];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[3];
float _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float;
Unity_Subtract_float(_SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float, _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float);
float _Split_83ebd8127a60c68790c7ccd13d68ea95_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_A_4_Float = 0;
float _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float;
Unity_Saturate_float(_Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float, _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float);
float _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float, _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float);
float _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 0.85, _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float);
float _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float;
Unity_Saturate_float(_Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float, _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float);
float _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float);
float _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float, _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float);
float _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 1, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float);
float _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float;
Unity_Add_float(_Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float, _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float);
float _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float = Vector1_17E53C12;
float _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float;
Unity_Multiply_float_float(_Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float, _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float, _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float);
float _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float = Vector1_A0EAD698;
float _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float;
Unity_Power_float(_Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float, _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float, _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float);
float _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float;
Unity_Multiply_float_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, 0.65, _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float);
float _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float;
Unity_Saturate_float(_Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float, _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float);
float4 _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4;
Unity_Lerp_float4(_Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4, _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4, (_Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float.xxxx), _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4);
float _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float = Vector1_36E8227;
float _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float;
Unity_Subtract_float(float(1), _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float, _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float);
float _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float;
Unity_Multiply_float_float(_Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float, 10, _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float);
float _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float = Vector1_E5C51606;
float _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float;
Unity_Divide_float(float(1), _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float);
float _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float, _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float);
float _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float, _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float);
float _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float;
Unity_Saturate_float(_Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float, _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float);
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float = IN.VertexColor[0];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_G_2_Float = IN.VertexColor[1];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_B_3_Float = IN.VertexColor[2];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_A_4_Float = IN.VertexColor[3];
float _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float;
Unity_Maximum_float(_Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float, _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float, _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float);
float _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float;
Unity_Multiply_float_float(_Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float, 2, _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float);
float _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float;
Unity_Power_float(_Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float, float(2), _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float);
float _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float;
Unity_Multiply_float_float(_Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float, 1.5, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float);
float _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float;
Unity_Subtract_float(_Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float, _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float);
UnityTexture2D _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Split_0aebe3f40460e5899da0f9738775cd80_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_0aebe3f40460e5899da0f9738775cd80_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_0aebe3f40460e5899da0f9738775cd80_A_4_Float = 0;
float _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float;
Unity_Multiply_float_float(_Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float, 0.5, _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float);
float _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float;
Unity_Add_float(float(1), _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float, _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float);
float4 _UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4 = IN.uv0;
float2 _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2 = Vector2_37B21477;
float2 _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2;
Unity_Multiply_float2_float2((_UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4.xy), _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2, _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2);
float2 _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2;
Unity_Multiply_float2_float2((_Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float.xx), _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2, _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2);
float2 _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2;
Unity_Preview_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2);
float2 _Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
float _Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float = Vector1_244B0600;
float2 _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2, (_Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float.xx), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2);
float2 _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2;
Unity_Multiply_float2_float2(float2(-0.1, 0.035), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2);
float2 _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2;
Unity_Add_float2(_Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2, _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2);
float4 _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.tex, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.samplerstate, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.GetTransformedUV(_Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2) );
if (_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4);
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.r;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.g;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_B_6_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.b;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_A_7_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.a;
UnityTexture2D _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, float2(-0.01, 0.05), _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2);
float _Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float = Vector1_F38B44AA;
float2 _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, (_Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float.xx), _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2);
float2 _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2;
Unity_Add_float2(_Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2, _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2, _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2);
float4 _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.tex, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.samplerstate, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.GetTransformedUV(_Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2) );
if (_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4);
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.r;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.g;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_B_6_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.b;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_A_7_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.a;
float _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float, _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float);
float _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float);
float _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float;
Unity_Add_float(_Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float, _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float);
float _Property_e95952e8670288869411626e7483404d_Out_0_Float = Vector1_C360A163;
float _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float;
Unity_Multiply_float_float(_Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float, _Property_e95952e8670288869411626e7483404d_Out_0_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float);
float _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float;
Unity_Step_float(_Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float, _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float);
float4 _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4);
float4 _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4;
Unity_Preview_float4(_Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4, _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4);
float4 _Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4 = Color_7D9A58EC;
float _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float;
Unity_Saturate_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float);
float4 _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4;
Unity_Lerp_float4(_Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4, _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4, (_Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float.xxxx), _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4);
float4 _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4);
float _Split_cdf92567be4e0587be8eabeb7b0acefc_R_1_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[0];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_G_2_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[1];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_B_3_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[2];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[3];
float _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float;
Unity_Subtract_float(float(1), _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float, _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float);
float _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float;
Unity_Saturate_float(_Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float, _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float);
float _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float, _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float);
float4 _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4, (_Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float.xxxx), _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4);
float4 _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4;
Unity_Preview_float4(_Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4, _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4);
float _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float = Vector1_46E42935;
float _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float;
Unity_Subtract_float(float(1), _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float, _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float);
float3 _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3;
NormalLerp_float((_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.xyz), (_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.xyz), _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float, _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3);
float _Split_6a5d5302af091b8c93ea83d56bb359d5_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_A_4_Float = 0;
float _Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float;
Unity_Subtract_float(float(1), _Split_6a5d5302af091b8c93ea83d56bb359d5_G_2_Float, _Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float);
float _Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float;
Unity_Multiply_float_float(_Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float, 0.5, _Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float);
float _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float;
Unity_Saturate_float(_Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float, _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float);
float _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float, _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float);
float3 _NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3;
Unity_NormalStrength_float(_NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3, _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float, _NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3);
float _Property_69a99fd328c02180bbfd9849ad10f057_Out_0_Float = Vector1_B9F56378;
float3 _NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3, _Property_69a99fd328c02180bbfd9849ad10f057_Out_0_Float, _NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3);
float _Split_7160bdacb63f1c82919254ec308e3488_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_7160bdacb63f1c82919254ec308e3488_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_7160bdacb63f1c82919254ec308e3488_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_7160bdacb63f1c82919254ec308e3488_A_4_Float = 0;
float _Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float;
Unity_Add_float(_Split_7160bdacb63f1c82919254ec308e3488_G_2_Float, float(0.75), _Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float);
float _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float;
Unity_Saturate_float(_Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float, _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float);
float3 _NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3, _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float, _NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3);
float _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float;
Unity_Subtract_float(float(1), _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float, _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float);
float3 _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3, _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float, _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3);
float4 _ScreenPosition_f74ad8fc29962780a3de5894cfa698c5_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float3 _Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3;
Unity_Preview_float3(_NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3, _Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3);
float3 _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3;
Unity_Normalize_float3(_Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3, _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3);
float _Split_b54baf2c806dd18faa612bd1f586c24a_R_1_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[0];
float _Split_b54baf2c806dd18faa612bd1f586c24a_G_2_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[1];
float _Split_b54baf2c806dd18faa612bd1f586c24a_B_3_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[2];
float _Split_b54baf2c806dd18faa612bd1f586c24a_A_4_Float = 0;
float2 _Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2 = float2(_Split_b54baf2c806dd18faa612bd1f586c24a_R_1_Float, _Split_b54baf2c806dd18faa612bd1f586c24a_G_2_Float);
float _Property_f8c0b1cc7dd32a8f83c31b1ce4a4e920_Out_0_Float = Vector1_A6A0BC26;
float2 _Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2, (_Property_f8c0b1cc7dd32a8f83c31b1ce4a4e920_Out_0_Float.xx), _Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2);
float _Property_a6131faf438a1b8894a3b406edcc5ebd_Out_0_Float = Vector1_17E53C12;
float _Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float;
Unity_Multiply_float_float(_Property_a6131faf438a1b8894a3b406edcc5ebd_Out_0_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float);
float _Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float;
Unity_Power_float(_Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float, float(2), _Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float);
float _Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float;
Unity_Saturate_float(_Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float, _Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float);
float2 _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2, (_Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float.xx), _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2);
float2 _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2, _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2, _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2);
float2 _Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2;
Unity_Add_float2((_ScreenPosition_f74ad8fc29962780a3de5894cfa698c5_Out_0_Vector4.xy), _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2, _Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2);
float3 _SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3;
Unity_SceneColor_float((float4(_Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2, 0.0, 1.0)), _SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3);
float3 _Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3;
Unity_Multiply_float3_float3(_SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3, (_Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float.xxx), _Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3);
float3 _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3;
Unity_Preview_float3(_Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3, _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3);
float _Property_1fc2ad6585a74187a59c88c55fbd590e_Out_0_Float = Vector1_3D886DA1;
float _Multiply_ab99e3f51ec8fc8791570f9004d63f68_Out_2_Float;
Unity_Multiply_float_float(0, 1.5, _Multiply_ab99e3f51ec8fc8791570f9004d63f68_Out_2_Float);
float _Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_ab99e3f51ec8fc8791570f9004d63f68_Out_2_Float, _Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float);
float _Multiply_b268713355bce98ea726cbf48f2b22b2_Out_2_Float;
Unity_Multiply_float_float(_Property_1fc2ad6585a74187a59c88c55fbd590e_Out_0_Float, _Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float, _Multiply_b268713355bce98ea726cbf48f2b22b2_Out_2_Float);
float _Split_6c36aeeb89846088a6f46beb89edeb01_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_6c36aeeb89846088a6f46beb89edeb01_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_6c36aeeb89846088a6f46beb89edeb01_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_6c36aeeb89846088a6f46beb89edeb01_A_4_Float = 0;
float _Add_5a62fb1ac5bef38a85ac2db7e9b9c621_Out_2_Float;
Unity_Add_float(float(0.15), _Split_6c36aeeb89846088a6f46beb89edeb01_G_2_Float, _Add_5a62fb1ac5bef38a85ac2db7e9b9c621_Out_2_Float);
float _Saturate_1aa5822a26860f898e713a38d5944219_Out_1_Float;
Unity_Saturate_float(_Add_5a62fb1ac5bef38a85ac2db7e9b9c621_Out_2_Float, _Saturate_1aa5822a26860f898e713a38d5944219_Out_1_Float);
float _Multiply_168cf0f7b082b086931db23b351f27db_Out_2_Float;
Unity_Multiply_float_float(_Multiply_b268713355bce98ea726cbf48f2b22b2_Out_2_Float, _Saturate_1aa5822a26860f898e713a38d5944219_Out_1_Float, _Multiply_168cf0f7b082b086931db23b351f27db_Out_2_Float);
float _Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float;
Unity_Subtract_float(float(1), _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float, _Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float);
float _Multiply_fb6714a555a32b898efd2b0fe002e34d_Out_2_Float;
Unity_Multiply_float_float(_Multiply_168cf0f7b082b086931db23b351f27db_Out_2_Float, _Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float, _Multiply_fb6714a555a32b898efd2b0fe002e34d_Out_2_Float);
float _Preview_0ead648dd722878590f431f5c578c1e1_Out_1_Float;
Unity_Preview_float(_Multiply_fb6714a555a32b898efd2b0fe002e34d_Out_2_Float, _Preview_0ead648dd722878590f431f5c578c1e1_Out_1_Float);
float _Property_db2c70ae908eaa82b28bc80aad11f92f_Out_0_Float = Vector1_47683D42;
float _Subtract_0b01d3ae1424c98791fc40db5c456faa_Out_2_Float;
Unity_Subtract_float(float(1), _Split_6c36aeeb89846088a6f46beb89edeb01_G_2_Float, _Subtract_0b01d3ae1424c98791fc40db5c456faa_Out_2_Float);
float _Multiply_3f242c97ee180089aaa704614c0403ec_Out_2_Float;
Unity_Multiply_float_float(_Subtract_0b01d3ae1424c98791fc40db5c456faa_Out_2_Float, 0.15, _Multiply_3f242c97ee180089aaa704614c0403ec_Out_2_Float);
float _Add_977a3c2b32c7a68e98d020b8ca06409b_Out_2_Float;
Unity_Add_float(_Multiply_3f242c97ee180089aaa704614c0403ec_Out_2_Float, float(1), _Add_977a3c2b32c7a68e98d020b8ca06409b_Out_2_Float);
float _Multiply_881ca79764d87589a60b678464e04d37_Out_2_Float;
Unity_Multiply_float_float(_Subtract_c1ea2691f2180282a3cb718986fd5b94_Out_2_Float, _Add_977a3c2b32c7a68e98d020b8ca06409b_Out_2_Float, _Multiply_881ca79764d87589a60b678464e04d37_Out_2_Float);
float _Multiply_19e292085664df8780de963844026a63_Out_2_Float;
Unity_Multiply_float_float(_Property_db2c70ae908eaa82b28bc80aad11f92f_Out_0_Float, _Multiply_881ca79764d87589a60b678464e04d37_Out_2_Float, _Multiply_19e292085664df8780de963844026a63_Out_2_Float);
float _Multiply_d1803de319716381bd2e252f385bd94c_Out_2_Float;
Unity_Multiply_float_float(_Subtract_83d339a8d0c641889785325be81b60c0_Out_2_Float, _Multiply_19e292085664df8780de963844026a63_Out_2_Float, _Multiply_d1803de319716381bd2e252f385bd94c_Out_2_Float);
float _Preview_9efdbaa90673428caf4fa565e36a200e_Out_1_Float;
Unity_Preview_float(_Multiply_d1803de319716381bd2e252f385bd94c_Out_2_Float, _Preview_9efdbaa90673428caf4fa565e36a200e_Out_1_Float);
surface.BaseColor = (_Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4.xyz);
surface.NormalTS = _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3;
surface.Emission = _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3;
surface.Metallic = _Preview_0ead648dd722878590f431f5c578c1e1_Out_1_Float;
surface.Smoothness = _Preview_9efdbaa90673428caf4fa565e36a200e_Out_1_Float;
surface.Occlusion = float(1);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = normalize(input.ecoMaterialNormalWS);      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
    output.VertexColor = input.color;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ShadowCaster"
    Tags
    {
        "LightMode" = "ShadowCaster"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask 0

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_NORMAL_WS
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_SHADOWCASTER


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
 float3 normalWS : INTERP0;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "MotionVectors"
    Tags
    {
        "LightMode" = "MotionVectors"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask RG

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.5
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_MOTION_VECTORS


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
#if SHADERPASS != SHADERPASS_META
EcoBendPosition(description.Position);
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthOnly"
    Tags
    {
        "LightMode" = "DepthOnly"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask R

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}

        // Terrain/water silhouette for the atmosphere; never used by the camera depth copy.
        Pass
{
    Name "EcoAtmosphereSurface"
    Tags
    {
        "LightMode" = "EcoAtmosphereSurface"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask R

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif








    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthNormals"
    Tags
    {
        "LightMode" = "DepthNormals"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_COLOR
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHNORMALS
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
float3 ecoMaterialNormalWS;
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
 float4 color;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
float3 ecoMaterialNormalWS : INTERP5;
 float4 positionCS : SV_POSITION;
 float4 tangentWS : INTERP0;
 float4 texCoord0 : INTERP1;
 float4 color : INTERP2;
 float3 positionWS : INTERP3;
 float3 normalWS : INTERP4;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Preview_float2(float2 In, out float2 Out)
{
    Out = In;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

// unity-custom-func-begin
void NormalLerp_float(float3 InA, float3 InB, float Ratio, out float3 Out){
float3 blend = normalize(float3(InA.rg + InB.rg, InA.b * InB.b));

Out = lerp(lerp(InA, blend, saturate(Ratio * 2)), InB, saturate((Ratio - 0.5) * 2));
}
// unity-custom-func-end

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 ecoMaterialNormalWS;
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.ecoMaterialNormalWS = TransformObjectToWorldNormal(IN.ObjectSpaceNormal);
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 NormalTS;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
UnityTexture2D _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Split_0aebe3f40460e5899da0f9738775cd80_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_0aebe3f40460e5899da0f9738775cd80_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_0aebe3f40460e5899da0f9738775cd80_A_4_Float = 0;
float _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float;
Unity_Multiply_float_float(_Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float, 0.5, _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float);
float _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float;
Unity_Add_float(float(1), _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float, _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float);
float4 _UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4 = IN.uv0;
float2 _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2 = Vector2_37B21477;
float2 _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2;
Unity_Multiply_float2_float2((_UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4.xy), _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2, _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2);
float2 _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2;
Unity_Multiply_float2_float2((_Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float.xx), _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2, _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2);
float2 _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2;
Unity_Preview_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2);
float2 _Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
float _Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float = Vector1_244B0600;
float2 _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2, (_Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float.xx), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2);
float2 _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2;
Unity_Multiply_float2_float2(float2(-0.1, 0.035), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2);
float2 _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2;
Unity_Add_float2(_Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2, _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2);
float4 _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.tex, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.samplerstate, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.GetTransformedUV(_Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2) );
if (_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4);
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.r;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.g;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_B_6_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.b;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_A_7_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.a;
UnityTexture2D _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, float2(-0.01, 0.05), _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2);
float _Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float = Vector1_F38B44AA;
float2 _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, (_Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float.xx), _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2);
float2 _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2;
Unity_Add_float2(_Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2, _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2, _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2);
float4 _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.tex, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.samplerstate, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.GetTransformedUV(_Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2) );
if (_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4);
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.r;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.g;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_B_6_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.b;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_A_7_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.a;
float _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float = Vector1_46E42935;
float _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float;
Unity_Subtract_float(float(1), _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float, _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float);
float3 _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3;
NormalLerp_float((_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.xyz), (_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.xyz), _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float, _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3);
float _Split_6a5d5302af091b8c93ea83d56bb359d5_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_6a5d5302af091b8c93ea83d56bb359d5_A_4_Float = 0;
float _Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float;
Unity_Subtract_float(float(1), _Split_6a5d5302af091b8c93ea83d56bb359d5_G_2_Float, _Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float);
float _Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float;
Unity_Multiply_float_float(_Subtract_aaf2a63f27ca2d8a8be7e9736d4948c0_Out_2_Float, 0.5, _Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float);
float _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float;
Unity_Saturate_float(_Multiply_28d54bccd8833c81a874be8a116624e6_Out_2_Float, _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float);
float _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_7207f3209fa07a8592cd60097392f623_Out_1_Float, _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float);
float3 _NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3;
Unity_NormalStrength_float(_NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3, _Subtract_c573fad41f8a3d848b37ef72de48115d_Out_2_Float, _NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3);
float _Property_69a99fd328c02180bbfd9849ad10f057_Out_0_Float = Vector1_B9F56378;
float3 _NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_0d2567dd46c79a86aa4694c71b3abfbe_Out_2_Vector3, _Property_69a99fd328c02180bbfd9849ad10f057_Out_0_Float, _NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3);
float _Split_7160bdacb63f1c82919254ec308e3488_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_7160bdacb63f1c82919254ec308e3488_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_7160bdacb63f1c82919254ec308e3488_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_7160bdacb63f1c82919254ec308e3488_A_4_Float = 0;
float _Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float;
Unity_Add_float(_Split_7160bdacb63f1c82919254ec308e3488_G_2_Float, float(0.75), _Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float);
float _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float;
Unity_Saturate_float(_Add_d8f0226f8b3c6587a0bf0cd4e5c202a2_Out_2_Float, _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float);
float3 _NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_0bbde4d4feceb88c8ee87ccfd2a96025_Out_2_Vector3, _Saturate_0563d3839558b58581f09b7c92754f94_Out_1_Float, _NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3);
float _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float = Vector1_36E8227;
float _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float;
Unity_Subtract_float(float(1), _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float, _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float);
float _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float;
Unity_Multiply_float_float(_Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float, 10, _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float);
float _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float);
float4 _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4 = IN.ScreenPosition;
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_R_1_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[0];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_G_2_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[1];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_B_3_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[2];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[3];
float _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float;
Unity_Subtract_float(_SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float, _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float);
float _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float = Vector1_E5C51606;
float _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float;
Unity_Divide_float(float(1), _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float);
float _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float, _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float);
float _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float, _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float);
float _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float;
Unity_Saturate_float(_Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float, _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float);
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float = IN.VertexColor[0];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_G_2_Float = IN.VertexColor[1];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_B_3_Float = IN.VertexColor[2];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_A_4_Float = IN.VertexColor[3];
float _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float;
Unity_Maximum_float(_Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float, _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float, _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float);
float _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float;
Unity_Multiply_float_float(_Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float, 2, _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float);
float _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float;
Unity_Power_float(_Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float, float(2), _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float);
float _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float;
Unity_Multiply_float_float(_Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float, 1.5, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float);
float _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float;
Unity_Subtract_float(_Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float, _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float);
float _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float, _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float);
float _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float);
float _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float;
Unity_Add_float(_Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float, _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float);
float _Property_e95952e8670288869411626e7483404d_Out_0_Float = Vector1_C360A163;
float _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float;
Unity_Multiply_float_float(_Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float, _Property_e95952e8670288869411626e7483404d_Out_0_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float);
float _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float;
Unity_Step_float(_Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float, _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float);
float _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float;
Unity_Subtract_float(float(1), _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float, _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float);
float3 _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3;
Unity_NormalStrength_float(_NormalStrength_876d1108c3108d80abd88b934f7362cf_Out_2_Vector3, _Subtract_7d3caf659d10ea849e4268c3c37a94b5_Out_2_Float, _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3);
surface.NormalTS = _NormalStrength_762f02739edcad83b3fb85636ca990c9_Out_2_Vector3;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = normalize(input.ecoMaterialNormalWS);      // we want a unit length Normal Vector node in shader graph
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
    output.VertexColor = input.color;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Meta"
    Tags
    {
        "LightMode" = "Meta"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature _ EDITOR_VISUALIZATION
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
#define ATTRIBUTES_NEED_INSTANCEID
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD2
#define VARYINGS_NEED_COLOR
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define _FOG_FRAGMENT 1
#define REQUIRE_DEPTH_TEXTURE
#define REQUIRE_OPAQUE_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
float3 ecoMaterialNormalWS;
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
 float4 texCoord1;
 float4 texCoord2;
 float4 color;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
float3 ecoMaterialNormalWS : INTERP6;
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 texCoord1 : INTERP1;
 float4 texCoord2 : INTERP2;
 float4 color : INTERP3;
 float3 positionWS : INTERP4;
 float3 normalWS : INTERP5;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.color.xyzw = input.color;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Preview_float2(float2 In, out float2 Out)
{
    Out = In;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

void Unity_Preview_float4(float4 In, out float4 Out)
{
    Out = In;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// unity-custom-func-begin
void NormalLerp_float(float3 InA, float3 InB, float Ratio, out float3 Out){
float3 blend = normalize(float3(InA.rg + InB.rg, InA.b * InB.b));

Out = lerp(lerp(InA, blend, saturate(Ratio * 2)), InB, saturate((Ratio - 0.5) * 2));
}
// unity-custom-func-end

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

void Unity_SceneColor_float(float4 UV, out float3 Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 ecoMaterialNormalWS;
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.ecoMaterialNormalWS = TransformObjectToWorldNormal(IN.ObjectSpaceNormal);
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 Emission;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4 = Color_7D9A58EC;
float _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float);
float4 _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4 = IN.ScreenPosition;
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_R_1_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[0];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_G_2_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[1];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_B_3_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[2];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[3];
float _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float;
Unity_Subtract_float(_SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float, _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float);
float _Split_83ebd8127a60c68790c7ccd13d68ea95_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_A_4_Float = 0;
float _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float;
Unity_Saturate_float(_Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float, _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float);
float _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float, _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float);
float _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 0.85, _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float);
float _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float;
Unity_Saturate_float(_Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float, _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float);
float _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float);
float _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float, _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float);
float _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 1, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float);
float _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float;
Unity_Add_float(_Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float, _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float);
float _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float = Vector1_17E53C12;
float _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float;
Unity_Multiply_float_float(_Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float, _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float, _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float);
float _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float = Vector1_A0EAD698;
float _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float;
Unity_Power_float(_Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float, _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float, _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float);
float _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float;
Unity_Multiply_float_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, 0.65, _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float);
float _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float;
Unity_Saturate_float(_Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float, _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float);
float4 _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4;
Unity_Lerp_float4(_Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4, _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4, (_Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float.xxxx), _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4);
float _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float = Vector1_36E8227;
float _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float;
Unity_Subtract_float(float(1), _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float, _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float);
float _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float;
Unity_Multiply_float_float(_Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float, 10, _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float);
float _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float = Vector1_E5C51606;
float _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float;
Unity_Divide_float(float(1), _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float);
float _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float, _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float);
float _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float, _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float);
float _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float;
Unity_Saturate_float(_Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float, _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float);
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float = IN.VertexColor[0];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_G_2_Float = IN.VertexColor[1];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_B_3_Float = IN.VertexColor[2];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_A_4_Float = IN.VertexColor[3];
float _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float;
Unity_Maximum_float(_Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float, _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float, _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float);
float _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float;
Unity_Multiply_float_float(_Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float, 2, _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float);
float _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float;
Unity_Power_float(_Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float, float(2), _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float);
float _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float;
Unity_Multiply_float_float(_Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float, 1.5, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float);
float _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float;
Unity_Subtract_float(_Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float, _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float);
UnityTexture2D _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Split_0aebe3f40460e5899da0f9738775cd80_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_0aebe3f40460e5899da0f9738775cd80_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_0aebe3f40460e5899da0f9738775cd80_A_4_Float = 0;
float _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float;
Unity_Multiply_float_float(_Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float, 0.5, _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float);
float _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float;
Unity_Add_float(float(1), _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float, _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float);
float4 _UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4 = IN.uv0;
float2 _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2 = Vector2_37B21477;
float2 _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2;
Unity_Multiply_float2_float2((_UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4.xy), _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2, _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2);
float2 _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2;
Unity_Multiply_float2_float2((_Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float.xx), _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2, _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2);
float2 _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2;
Unity_Preview_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2);
float2 _Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
float _Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float = Vector1_244B0600;
float2 _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2, (_Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float.xx), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2);
float2 _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2;
Unity_Multiply_float2_float2(float2(-0.1, 0.035), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2);
float2 _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2;
Unity_Add_float2(_Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2, _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2);
float4 _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.tex, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.samplerstate, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.GetTransformedUV(_Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2) );
if (_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4);
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.r;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.g;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_B_6_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.b;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_A_7_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.a;
UnityTexture2D _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, float2(-0.01, 0.05), _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2);
float _Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float = Vector1_F38B44AA;
float2 _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, (_Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float.xx), _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2);
float2 _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2;
Unity_Add_float2(_Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2, _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2, _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2);
float4 _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.tex, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.samplerstate, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.GetTransformedUV(_Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2) );
if (_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4);
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.r;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.g;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_B_6_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.b;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_A_7_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.a;
float _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float, _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float);
float _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float);
float _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float;
Unity_Add_float(_Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float, _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float);
float _Property_e95952e8670288869411626e7483404d_Out_0_Float = Vector1_C360A163;
float _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float;
Unity_Multiply_float_float(_Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float, _Property_e95952e8670288869411626e7483404d_Out_0_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float);
float _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float;
Unity_Step_float(_Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float, _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float);
float4 _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4);
float4 _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4;
Unity_Preview_float4(_Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4, _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4);
float4 _Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4 = Color_7D9A58EC;
float _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float;
Unity_Saturate_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float);
float4 _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4;
Unity_Lerp_float4(_Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4, _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4, (_Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float.xxxx), _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4);
float4 _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4);
float _Split_cdf92567be4e0587be8eabeb7b0acefc_R_1_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[0];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_G_2_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[1];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_B_3_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[2];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[3];
float _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float;
Unity_Subtract_float(float(1), _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float, _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float);
float _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float;
Unity_Saturate_float(_Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float, _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float);
float _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float, _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float);
float4 _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4, (_Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float.xxxx), _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4);
float4 _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4;
Unity_Preview_float4(_Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4, _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4);
float4 _ScreenPosition_f74ad8fc29962780a3de5894cfa698c5_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float = Vector1_46E42935;
float _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float;
Unity_Subtract_float(float(1), _Property_4d2ed6884be807898ef7a362238ed406_Out_0_Float, _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float);
float3 _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3;
NormalLerp_float((_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.xyz), (_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.xyz), _Subtract_60e646cf3b2fe381bda65127e6e21beb_Out_2_Float, _NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3);
float3 _Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3;
Unity_Preview_float3(_NormalLerpCustomFunction_9f5e4b06ec944e8a84f7b16c6b04b8df_Out_3_Vector3, _Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3);
float3 _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3;
Unity_Normalize_float3(_Preview_67e4d0f82fdf638db4ebdcd48a663558_Out_1_Vector3, _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3);
float _Split_b54baf2c806dd18faa612bd1f586c24a_R_1_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[0];
float _Split_b54baf2c806dd18faa612bd1f586c24a_G_2_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[1];
float _Split_b54baf2c806dd18faa612bd1f586c24a_B_3_Float = _Normalize_0d313f60f1881687b09b34cd58deea63_Out_1_Vector3[2];
float _Split_b54baf2c806dd18faa612bd1f586c24a_A_4_Float = 0;
float2 _Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2 = float2(_Split_b54baf2c806dd18faa612bd1f586c24a_R_1_Float, _Split_b54baf2c806dd18faa612bd1f586c24a_G_2_Float);
float _Property_f8c0b1cc7dd32a8f83c31b1ce4a4e920_Out_0_Float = Vector1_A6A0BC26;
float2 _Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2, (_Property_f8c0b1cc7dd32a8f83c31b1ce4a4e920_Out_0_Float.xx), _Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2);
float _Property_a6131faf438a1b8894a3b406edcc5ebd_Out_0_Float = Vector1_17E53C12;
float _Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float;
Unity_Multiply_float_float(_Property_a6131faf438a1b8894a3b406edcc5ebd_Out_0_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float);
float _Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float;
Unity_Power_float(_Multiply_43acd4480cf1ef8d95b145da38be26a0_Out_2_Float, float(2), _Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float);
float _Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float;
Unity_Saturate_float(_Power_24a304b17b28c88dbaddaf20b843178a_Out_2_Float, _Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float);
float2 _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_b1235832e9249187a672080cf2c1770b_Out_2_Vector2, (_Saturate_f4e5432bc1b2088fa4047ef743318e6e_Out_1_Float.xx), _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2);
float2 _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_e5a3f7832ea68a87a41614e1dab97954_Out_0_Vector2, _Multiply_9bed97a80f1f34889ae9e62671b5a712_Out_2_Vector2, _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2);
float2 _Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2;
Unity_Add_float2((_ScreenPosition_f74ad8fc29962780a3de5894cfa698c5_Out_0_Vector4.xy), _Multiply_04091cdbf8ff3584b29951521d2b909e_Out_2_Vector2, _Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2);
float3 _SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3;
Unity_SceneColor_float((float4(_Add_4e296c5be9414a8cb431adca7fb10872_Out_2_Vector2, 0.0, 1.0)), _SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3);
float3 _Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3;
Unity_Multiply_float3_float3(_SceneColor_d85979d75cf5d9869511b11cfdd62a82_Out_1_Vector3, (_Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float.xxx), _Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3);
float3 _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3;
Unity_Preview_float3(_Multiply_232ebe37e694438cac3ce4da3757f051_Out_2_Vector3, _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3);
surface.BaseColor = (_Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4.xyz);
surface.Emission = _Preview_7afbf49d89518f8ca2d2bfffb783a8ea_Out_1_Vector3;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = normalize(input.ecoMaterialNormalWS);      // we want a unit length Normal Vector node in shader graph


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
    output.VertexColor = input.color;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "SceneSelectionPass"
    Tags
    {
        "LightMode" = "SceneSelectionPass"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENESELECTIONPASS 1
#define ALPHA_CLIP_THRESHOLD 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ScenePickingPass"
    Tags
    {
        "LightMode" = "Picking"
    }

// Render State
Cull Back

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_COLOR
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENEPICKINGPASS 1
#define ALPHA_CLIP_THRESHOLD 1
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
float3 ecoMaterialNormalWS;
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
 float4 color;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
float3 ecoMaterialNormalWS : INTERP4;
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 color : INTERP1;
 float3 positionWS : INTERP2;
 float3 normalWS : INTERP3;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Preview_float2(float2 In, out float2 Out)
{
    Out = In;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

void Unity_Preview_float4(float4 In, out float4 Out)
{
    Out = In;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 ecoMaterialNormalWS;
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.ecoMaterialNormalWS = TransformObjectToWorldNormal(IN.ObjectSpaceNormal);
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4 = Color_7D9A58EC;
float _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float);
float4 _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4 = IN.ScreenPosition;
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_R_1_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[0];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_G_2_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[1];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_B_3_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[2];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[3];
float _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float;
Unity_Subtract_float(_SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float, _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float);
float _Split_83ebd8127a60c68790c7ccd13d68ea95_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_A_4_Float = 0;
float _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float;
Unity_Saturate_float(_Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float, _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float);
float _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float, _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float);
float _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 0.85, _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float);
float _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float;
Unity_Saturate_float(_Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float, _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float);
float _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float);
float _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float, _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float);
float _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 1, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float);
float _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float;
Unity_Add_float(_Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float, _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float);
float _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float = Vector1_17E53C12;
float _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float;
Unity_Multiply_float_float(_Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float, _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float, _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float);
float _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float = Vector1_A0EAD698;
float _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float;
Unity_Power_float(_Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float, _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float, _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float);
float _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float;
Unity_Multiply_float_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, 0.65, _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float);
float _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float;
Unity_Saturate_float(_Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float, _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float);
float4 _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4;
Unity_Lerp_float4(_Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4, _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4, (_Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float.xxxx), _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4);
float _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float = Vector1_36E8227;
float _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float;
Unity_Subtract_float(float(1), _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float, _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float);
float _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float;
Unity_Multiply_float_float(_Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float, 10, _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float);
float _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float = Vector1_E5C51606;
float _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float;
Unity_Divide_float(float(1), _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float);
float _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float, _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float);
float _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float, _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float);
float _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float;
Unity_Saturate_float(_Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float, _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float);
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float = IN.VertexColor[0];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_G_2_Float = IN.VertexColor[1];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_B_3_Float = IN.VertexColor[2];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_A_4_Float = IN.VertexColor[3];
float _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float;
Unity_Maximum_float(_Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float, _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float, _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float);
float _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float;
Unity_Multiply_float_float(_Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float, 2, _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float);
float _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float;
Unity_Power_float(_Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float, float(2), _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float);
float _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float;
Unity_Multiply_float_float(_Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float, 1.5, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float);
float _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float;
Unity_Subtract_float(_Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float, _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float);
UnityTexture2D _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Split_0aebe3f40460e5899da0f9738775cd80_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_0aebe3f40460e5899da0f9738775cd80_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_0aebe3f40460e5899da0f9738775cd80_A_4_Float = 0;
float _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float;
Unity_Multiply_float_float(_Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float, 0.5, _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float);
float _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float;
Unity_Add_float(float(1), _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float, _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float);
float4 _UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4 = IN.uv0;
float2 _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2 = Vector2_37B21477;
float2 _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2;
Unity_Multiply_float2_float2((_UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4.xy), _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2, _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2);
float2 _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2;
Unity_Multiply_float2_float2((_Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float.xx), _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2, _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2);
float2 _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2;
Unity_Preview_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2);
float2 _Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
float _Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float = Vector1_244B0600;
float2 _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2, (_Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float.xx), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2);
float2 _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2;
Unity_Multiply_float2_float2(float2(-0.1, 0.035), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2);
float2 _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2;
Unity_Add_float2(_Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2, _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2);
float4 _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.tex, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.samplerstate, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.GetTransformedUV(_Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2) );
if (_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4);
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.r;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.g;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_B_6_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.b;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_A_7_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.a;
UnityTexture2D _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, float2(-0.01, 0.05), _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2);
float _Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float = Vector1_F38B44AA;
float2 _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, (_Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float.xx), _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2);
float2 _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2;
Unity_Add_float2(_Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2, _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2, _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2);
float4 _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.tex, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.samplerstate, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.GetTransformedUV(_Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2) );
if (_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4);
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.r;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.g;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_B_6_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.b;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_A_7_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.a;
float _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float, _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float);
float _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float);
float _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float;
Unity_Add_float(_Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float, _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float);
float _Property_e95952e8670288869411626e7483404d_Out_0_Float = Vector1_C360A163;
float _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float;
Unity_Multiply_float_float(_Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float, _Property_e95952e8670288869411626e7483404d_Out_0_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float);
float _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float;
Unity_Step_float(_Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float, _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float);
float4 _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4);
float4 _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4;
Unity_Preview_float4(_Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4, _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4);
float4 _Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4 = Color_7D9A58EC;
float _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float;
Unity_Saturate_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float);
float4 _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4;
Unity_Lerp_float4(_Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4, _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4, (_Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float.xxxx), _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4);
float4 _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4);
float _Split_cdf92567be4e0587be8eabeb7b0acefc_R_1_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[0];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_G_2_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[1];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_B_3_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[2];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[3];
float _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float;
Unity_Subtract_float(float(1), _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float, _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float);
float _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float;
Unity_Saturate_float(_Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float, _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float);
float _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float, _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float);
float4 _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4, (_Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float.xxxx), _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4);
float4 _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4;
Unity_Preview_float4(_Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4, _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4);
surface.BaseColor = (_Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4.xyz);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = normalize(input.ecoMaterialNormalWS);      // we want a unit length Normal Vector node in shader graph


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
    output.VertexColor = input.color;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Universal 2D"
    Tags
    {
        "LightMode" = "Universal2D"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
// GraphKeywords: <None>

// Defines

#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_COLOR
#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_2D
#define REQUIRE_DEPTH_TEXTURE


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
float3 ecoMaterialNormalWS;
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 texCoord0;
 float4 color;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 WorldSpaceNormal;
 float3 WorldSpacePosition;
 float4 ScreenPosition;
 float2 NDCPosition;
 float2 PixelPosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float3 TimeParameters;
};
struct PackedVaryings
{
float3 ecoMaterialNormalWS : INTERP4;
 float4 positionCS : SV_POSITION;
 float4 texCoord0 : INTERP0;
 float4 color : INTERP1;
 float3 positionWS : INTERP2;
 float3 normalWS : INTERP3;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}


// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 Color_F01C36BF;
float4 Color_7D9A58EC;
float Vector1_47683D42;
float Vector1_3D886DA1;
float4 Texture2D_6490A223_TexelSize;
float2 Vector2_37B21477;
float Vector1_F38B44AA;
float Vector1_46E42935;
float Vector1_244B0600;
float _WaveFrequency;
float _WaveScale;
float _WaveSpeed;
float Vector1_B9F56378;
float Vector1_A6A0BC26;
float Vector1_36E8227;
float Vector1_C360A163;
float Vector1_17E53C12;
float Vector1_A0EAD698;
float Vector1_E5C51606;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(Texture2D_6490A223);
SAMPLER(samplerTexture2D_6490A223);

// Graph Includes
// GraphIncludes: <None>

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Step_float(float Edge, float In, out float Out)
{
    Out = step(Edge, In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Cosine_float(float In, out float Out)
{
    Out = cos(In);
}

void Unity_Preview_float3(float3 In, out float3 Out)
{
    Out = In;
}

void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
{
    if (unity_OrthoParams.w == 1.0)
    {
        Out = LinearEyeDepth(ComputeWorldSpacePosition(UV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
    }
    else
    {
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
    }
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Preview_float2(float2 In, out float2 Out)
{
    Out = In;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

void Unity_Preview_float4(float4 In, out float4 Out)
{
    Out = In;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 ecoMaterialNormalWS;
float3 Position;
float3 Normal;
float3 Tangent;
};

#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
description.ecoMaterialNormalWS = TransformObjectToWorldNormal(IN.ObjectSpaceNormal);
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_43b1c0bc10ced4888d815e0fc19eeda8_A_4_Float = 0;
float _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float;
Unity_Step_float(float(0.5), _Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float);
float _Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float = _WaveSpeed;
float _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float;
Unity_Multiply_float_float(_Property_257d0b6101a2a989ba2ae2634ee637fc_Out_0_Float, IN.TimeParameters.x, _Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float);
float _Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float = _WaveFrequency;
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float = IN.WorldSpacePosition[0];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_G_2_Float = IN.WorldSpacePosition[1];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float = IN.WorldSpacePosition[2];
float _Split_5422d82f034ecb8c8b860aab2a5e4ec7_A_4_Float = 0;
float _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float;
Unity_Multiply_float_float(_Property_790dccf35778e58a99af8c306d10da8c_Out_0_Float, _Split_5422d82f034ecb8c8b860aab2a5e4ec7_R_1_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float);
float _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_3e609e89b8476b829989b51afa7a5369_Out_2_Float, _Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float);
float _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float;
Unity_Sine_float(_Add_43dff4af387ada8e91bd38017a7ea280_Out_2_Float, _Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float);
float _Property_8a0277777cfaae858696acc5205705de_Out_0_Float = _WaveFrequency;
float _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float;
Unity_Multiply_float_float(_Split_5422d82f034ecb8c8b860aab2a5e4ec7_B_3_Float, _Property_8a0277777cfaae858696acc5205705de_Out_0_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float);
float _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float;
Unity_Add_float(_Multiply_f7fb969b738a198099f668892c080f66_Out_2_Float, _Multiply_13249a46bb15a087aba01d3f51a25b4f_Out_2_Float, _Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float);
float _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float;
Unity_Cosine_float(_Add_067d7a1c0c937b849d4bdb80ce2f4574_Out_2_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float);
float _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float;
Unity_Add_float(_Sine_ee9035d916e0c587a7dc9bc52b07f103_Out_1_Float, _Cosine_6aa0b00e559ab48dbc3480ade1e8422b_Out_1_Float, _Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float);
float _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float = _WaveScale;
float _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float;
Unity_Multiply_float_float(_Add_8860353d6edef887ada5f257d78a9da3_Out_2_Float, _Property_c676b0b999f1078d980b778220f22b9f_Out_0_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float);
float _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float;
Unity_Multiply_float_float(_Step_ca72c9e627abf28484ef6c3e1eaa7b3a_Out_2_Float, _Multiply_5ee0d2891d7f3e8c9d5fe1205387286d_Out_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float);
float _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float;
Unity_Add_float(_Split_43b1c0bc10ced4888d815e0fc19eeda8_G_2_Float, _Multiply_329e7824d7bdc88f8f6d2f428a0cf310_Out_2_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float);
float3 _Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3 = float3(_Split_43b1c0bc10ced4888d815e0fc19eeda8_R_1_Float, _Add_22bcf2caf8dd2e8b9f97fb73c156db09_Out_2_Float, _Split_43b1c0bc10ced4888d815e0fc19eeda8_B_3_Float);
float3 _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
Unity_Preview_float3(_Vector3_6b3583d72a02268c84ea2c699d24cbc9_Out_0_Vector3, _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3);
description.Position = _Preview_def36a597691b18e86332d21712c79f3_Out_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
#if SHADERPASS != SHADERPASS_META
float4 ecoTangent = float4(description.Tangent, 1);
EcoBendVertex(description.Position, description.Normal, ecoTangent);
description.Tangent = ecoTangent.xyz;
#endif
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.ecoMaterialNormalWS = input.ecoMaterialNormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float4 _Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4 = Color_7D9A58EC;
float _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float;
Unity_SceneDepth_Eye_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float);
float4 _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4 = IN.ScreenPosition;
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_R_1_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[0];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_G_2_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[1];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_B_3_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[2];
float _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float = _ScreenPosition_49bcf4c1806d04878850144cf4ee7ed4_Out_0_Vector4[3];
float _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float;
Unity_Subtract_float(_SceneDepth_7485ea9ea899d28c8be7683d40ccf1b0_Out_1_Float, _Split_53b9ca21e8ed1f82979cd1e8ece4aa6c_A_4_Float, _Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float);
float _Split_83ebd8127a60c68790c7ccd13d68ea95_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_83ebd8127a60c68790c7ccd13d68ea95_A_4_Float = 0;
float _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float;
Unity_Saturate_float(_Split_83ebd8127a60c68790c7ccd13d68ea95_G_2_Float, _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float);
float _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_818c78fd43d91f81bdb1e3b2f3eabde3_Out_1_Float, _Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float);
float _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 0.85, _Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float);
float _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float;
Unity_Saturate_float(_Multiply_1e1cb6878210a486a903742435ddc18c_Out_2_Float, _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float);
float _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_902a900c9b5303848c037d74ffbf4e29_Out_1_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float);
float _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Subtract_34e4cb1ada6dbd87acde22cd6f36ecb3_Out_2_Float, _Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float);
float _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float;
Unity_Multiply_float_float(_Subtract_f65f3a663384dd8f83ec40bfcfd4300d_Out_2_Float, 1, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float);
float _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float;
Unity_Add_float(_Multiply_cce0044526f8b688a1d2f3bedf4b3d05_Out_2_Float, _Multiply_9f91fc1121bbfb84ad8d55482a360699_Out_2_Float, _Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float);
float _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float = Vector1_17E53C12;
float _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float;
Unity_Multiply_float_float(_Add_4be56ae8ecfce684964584253b11adb3_Out_2_Float, _Property_ff42beb7853a928989391173ac646dfb_Out_0_Float, _Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float);
float _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float = Vector1_A0EAD698;
float _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float;
Unity_Power_float(_Multiply_50279ad7731b8c868b030088dc3f9fa6_Out_2_Float, _Property_cf1683308d31668bb122f7084240ecbb_Out_0_Float, _Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float);
float _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float;
Unity_Multiply_float_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, 0.65, _Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float);
float _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float;
Unity_Saturate_float(_Multiply_18ef3b50a1bd7882a7d60cee57bc352f_Out_2_Float, _Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float);
float4 _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4;
Unity_Lerp_float4(_Property_fc4f114daf53e88ea27d789378f0fdfd_Out_0_Vector4, _Property_6c51fcafd60a798ab8096fbc9eb37d77_Out_0_Vector4, (_Saturate_0869f90f42a5ab829b8e62cab097d8e0_Out_1_Float.xxxx), _Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4);
float _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float = Vector1_36E8227;
float _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float;
Unity_Subtract_float(float(1), _Property_0f32c8b82687fc8d85d801c1c01a46f4_Out_0_Float, _Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float);
float _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float;
Unity_Multiply_float_float(_Subtract_923ca40f8fd9858cbc3dd636c9a15e08_Out_2_Float, 10, _Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float);
float _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float = Vector1_E5C51606;
float _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float;
Unity_Divide_float(float(1), _Property_cf3dcc814c1e878a95470325599355c1_Out_0_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float);
float _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float;
Unity_Multiply_float_float(_Subtract_d269e1acedc0568a8494d58b778048d8_Out_2_Float, _Divide_9cc89c4316557485aa8297a7c21f9fb1_Out_2_Float, _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float);
float _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float;
Unity_Subtract_float(float(1), _Multiply_5c0d4568152db080a53baa6e6001ec87_Out_2_Float, _Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float);
float _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float;
Unity_Saturate_float(_Subtract_ae6fcb80a20c888a9cef5834d1e645dd_Out_2_Float, _Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float);
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float = IN.VertexColor[0];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_G_2_Float = IN.VertexColor[1];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_B_3_Float = IN.VertexColor[2];
float _Split_220f68bdfeeb448a81eba9e0d4b8006f_A_4_Float = IN.VertexColor[3];
float _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float;
Unity_Maximum_float(_Saturate_d6d8d3e037039d839b776a0514ea5267_Out_1_Float, _Split_220f68bdfeeb448a81eba9e0d4b8006f_R_1_Float, _Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float);
float _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float;
Unity_Multiply_float_float(_Maximum_5b6a84de30810684bb9af371ea21dd79_Out_2_Float, 2, _Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float);
float _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float;
Unity_Power_float(_Multiply_fa46f344c190678797caf2e162ae78fd_Out_2_Float, float(2), _Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float);
float _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float;
Unity_Multiply_float_float(_Power_24af7030999ec78283ab549bcd4fdb1c_Out_2_Float, 1.5, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float);
float _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float;
Unity_Subtract_float(_Multiply_519867820c64b880865d59e41ac8a3e2_Out_2_Float, _Multiply_9d05b93335f6b8889cb6e24dd397cf2b_Out_2_Float, _Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float);
UnityTexture2D _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float _Split_0aebe3f40460e5899da0f9738775cd80_R_1_Float = IN.WorldSpaceNormal[0];
float _Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float = IN.WorldSpaceNormal[1];
float _Split_0aebe3f40460e5899da0f9738775cd80_B_3_Float = IN.WorldSpaceNormal[2];
float _Split_0aebe3f40460e5899da0f9738775cd80_A_4_Float = 0;
float _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float;
Unity_Multiply_float_float(_Split_0aebe3f40460e5899da0f9738775cd80_G_2_Float, 0.5, _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float);
float _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float;
Unity_Add_float(float(1), _Multiply_e4c4968b3df76f8cbd83f0ee9a5a92e8_Out_2_Float, _Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float);
float4 _UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4 = IN.uv0;
float2 _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2 = Vector2_37B21477;
float2 _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2;
Unity_Multiply_float2_float2((_UV_d63042c3ef4b3f8fbe79907807fecd59_Out_0_Vector4.xy), _Property_3aecbe0d9a886781a672836fbb5c4a8a_Out_0_Vector2, _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2);
float2 _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2;
Unity_Multiply_float2_float2((_Add_8f6902e958af09859688ec7b2d9c45f9_Out_2_Float.xx), _Multiply_b598337f5e1f9e828828db2a297bda6d_Out_2_Vector2, _Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2);
float2 _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2;
Unity_Preview_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, _Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2);
float2 _Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
float _Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float = Vector1_244B0600;
float2 _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_3c04d76ca079768fbe37c05bc31de1fd_Out_0_Vector2, (_Property_2adc8fd11d60df88a1b7cb537d2f0b3b_Out_0_Float.xx), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2);
float2 _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2;
Unity_Multiply_float2_float2(float2(-0.1, 0.035), _Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2);
float2 _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2;
Unity_Add_float2(_Preview_c2dfd1cbc4959c84bdf47bc8b40b5760_Out_1_Vector2, _Multiply_8cd3383f374a10898bc61771ad99718a_Out_2_Vector2, _Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2);
float4 _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.tex, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.samplerstate, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.GetTransformedUV(_Add_8b7fa80f04802a8ca6dfa87a7bd471bb_Out_2_Vector2) );
if (_Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4, _Property_e355e6e8df0d608894cf4c28d6fd0387_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4);
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.r;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.g;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_B_6_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.b;
float _SampleTexture2D_d11716242772eb8a9feec47339f081a6_A_7_Float = _SampleTexture2D_d11716242772eb8a9feec47339f081a6_RGBA_0_Vector4.a;
UnityTexture2D _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D = UnityBuildTexture2DStructInternal(Texture2D_6490A223, samplerTexture2D_6490A223, Texture2D_6490A223_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_3822c4d053cb138bbc1c480f2027fefe_Out_2_Vector2, float2(-0.01, 0.05), _Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2);
float _Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float = Vector1_F38B44AA;
float2 _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2;
Unity_Multiply_float2_float2(_Multiply_103fe487915fb38e9e8eccf3935b12da_Out_2_Vector2, (_Property_a2c2f9cea8904b8a988ff8503cbcb049_Out_0_Float.xx), _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2);
float2 _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2;
Unity_Add_float2(_Multiply_f8ce72e0d4e76e84a638d583534e6ad0_Out_2_Vector2, _Multiply_92aad99a7cf4b38b8e0ef778f0e5a59c_Out_2_Vector2, _Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2);
float4 _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.tex, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.samplerstate, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.GetTransformedUV(_Add_e8edcbaaef91a681a52998ca70f0f429_Out_2_Vector2) );
if (_Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4, _Property_81e40b74da85fa88a50e525eb00a7bb0_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4);
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.r;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.g;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_B_6_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.b;
float _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_A_7_Float = _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_RGBA_0_Vector4.a;
float _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_R_4_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_G_5_Float, _Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float);
float _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_d11716242772eb8a9feec47339f081a6_G_5_Float, _SampleTexture2D_11a78f09742a8482ac4d27b486c9ac98_R_4_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float);
float _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float;
Unity_Add_float(_Multiply_b56e018660493d8eb861161d6c9c33ba_Out_2_Float, _Multiply_e08f216034604f879b4455d3acf54ed7_Out_2_Float, _Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float);
float _Property_e95952e8670288869411626e7483404d_Out_0_Float = Vector1_C360A163;
float _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float;
Unity_Multiply_float_float(_Add_97ea7bbd7e3c2e809b617881d780ba53_Out_2_Float, _Property_e95952e8670288869411626e7483404d_Out_0_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float);
float _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float;
Unity_Step_float(_Subtract_134d42fc94a1468aaa56d8b0f40f4803_Out_2_Float, _Multiply_a7e7cb8b60a493838a3d79b40a9f2289_Out_2_Float, _Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float);
float4 _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_5d510e975686f58d9d660e4dad3ec8fc_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4);
float4 _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4;
Unity_Preview_float4(_Lerp_a92cc7fb8b07a38488ccbe223a53c2fb_Out_3_Vector4, _Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4);
float4 _Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4 = Color_F01C36BF;
float4 _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4 = Color_7D9A58EC;
float _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float;
Unity_Saturate_float(_Power_134c81dd7479c984b7c807f9df79fa7f_Out_2_Float, _Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float);
float4 _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4;
Unity_Lerp_float4(_Property_a241c26feb17f88eba2560a374e58644_Out_0_Vector4, _Property_b2ed8295ee91188980f2ffe5b5f75c97_Out_0_Vector4, (_Saturate_5ce4208f9314d88c8baabe4b124f1cf3_Out_1_Float.xxxx), _Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4);
float4 _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_24dc363448ce518ca6d0a8370aa98258_Out_3_Vector4, float4(1, 1, 1, 1), (_Step_8d51b0d50fb4bb87a734cdaf791550c3_Out_2_Float.xxxx), _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4);
float _Split_cdf92567be4e0587be8eabeb7b0acefc_R_1_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[0];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_G_2_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[1];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_B_3_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[2];
float _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float = _Lerp_57aedbf210daba80bde3d3ae64624298_Out_3_Vector4[3];
float _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float;
Unity_Subtract_float(float(1), _Split_cdf92567be4e0587be8eabeb7b0acefc_A_4_Float, _Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float);
float _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float;
Unity_Saturate_float(_Subtract_b9a0a5a58e2ee284bdd754a6033b9f4a_Out_2_Float, _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float);
float _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float;
Unity_Subtract_float(float(1), _Saturate_0bfbcac1e9385c87b8de244bb4960c16_Out_1_Float, _Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float);
float4 _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4;
Unity_Multiply_float4_float4(_Preview_64f87ab4b9751684866aa3a628785fc6_Out_1_Vector4, (_Subtract_8c9d2c8b95617384a37093ddf6daa364_Out_2_Float.xxxx), _Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4);
float4 _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4;
Unity_Preview_float4(_Multiply_581a1df130552b81925ae20711cb34e5_Out_2_Vector4, _Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4);
surface.BaseColor = (_Preview_547a9e1daa254b89abe5549ed7a53927_Out_1_Vector4.xyz);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    

    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);


    output.WorldSpaceNormal = normalize(input.ecoMaterialNormalWS);      // we want a unit length Normal Vector node in shader graph


    output.WorldSpacePosition = input.positionWS;
    output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if UNITY_UV_STARTS_AT_TOP
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #else
    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
    #endif

    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
    output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    output.uv0 = input.texCoord0;
    output.VertexColor = input.color;
#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
    output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
}
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}