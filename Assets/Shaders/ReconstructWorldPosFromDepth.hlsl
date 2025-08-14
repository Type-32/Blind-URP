#ifndef CUSTOM_RECONSTRUCT_WORLD_POS
#define CUSTOM_RECONSTRUCT_WORLD_POS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

void ReconstructWorldPos_float(float2 UV, out float3 WorldPos)
{
    float deviceDepth = SampleSceneDepth(UV);
    WorldPos = ComputeWorldSpacePosition(UV, deviceDepth, UNITY_MATRIX_I_VP);
}

#endif