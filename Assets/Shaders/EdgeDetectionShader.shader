Shader "Hidden/Edge Detection"
{
    Properties
    {
        _OutlineThickness ("Outline Thickness", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _BaseColor ("Base Color", Color) = (0,0,0,1)
        _DepthAmplifier("Depth Amiplifier", Float) = 1.0
        _BoundaryColor("Boundary Color", Color) = (0,0,1,1)
        _BoundaryWidth("Boudnary Width Power", Float) = 5.0
        
        // New uniforms to be controlled by the C# script
        _BoundaryOrigin ("Boundary Origin", Vector) = (0,0,0,0)
        _BoundaryRange ("Boundary Range", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "EDGE DETECTION OUTLINE"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            // Uniforms from the C# script
            float _OutlineThickness;
            float4 _OutlineColor;
            float4 _BaseColor;
            float _DepthAmplifier;
            float4 _BoundaryColor;
            float _BoundaryWidth;
            float4 _BoundaryOrigin;
            float _BoundaryRange;
            
            #pragma vertex Vert
            #pragma fragment frag

            float RobertsCross(float3 samples[4])
            {
                const float3 difference_1 = samples[1] - samples[2];
                const float3 difference_2 = samples[0] - samples[3];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            float RobertsCross(float samples[4])
            {
                const float difference_1 = samples[1] - samples[2];
                const float difference_2 = samples[0] - samples[3];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }
            
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return SampleSceneNormals(uv) * 0.5 + 0.5;
            }

            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }

            float SphereMask(float3 center, float3 input, float radius)
            {
                return distance(center, input) < radius ? 1.0f : 0.0f;
            }

            float SpatialFresnel (float3 center, float3 input,float radius, float power, float intensity)
            {
                float dist = distance(center, input);
                float percentage = pow(dist / radius, power);

                return saturate(percentage * intensity);
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                // Screen-space coordinates which we will use to sample.
                float2 uv = IN.texcoord;
                float2 texel_size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                // reconstructing world space position from scene depth
                float2 screenUV = IN.positionCS.xy / _ScaledScreenParams.xy;
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                float3 worldPos = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                
                // === MODIFIED SECTION ===
                // Use the new uniforms instead of hardcoded values for the sonar effect.
                float sphereMask = SphereMask(_BoundaryOrigin.xyz, worldPos, _BoundaryRange);
                half4 sonarLight = _BoundaryColor * SpatialFresnel(_BoundaryOrigin.xyz, worldPos, _BoundaryRange, _BoundaryWidth,  2) * sphereMask;
                // ========================
                
                // Generate 4 diagonally placed samples.
                const float half_width_f = floor(_OutlineThickness * 0.5);
                const float half_width_c = ceil(_OutlineThickness * 0.5);

                float2 uvs[4];
                uvs[0] = uv + texel_size * float2(half_width_f, half_width_c) * float2(-1, 1);  // top left
                uvs[1] = uv + texel_size * float2(half_width_c, half_width_c) * float2(1, 1);   // top right
                uvs[2] = uv + texel_size * float2(half_width_f, half_width_f) * float2(-1, -1); // bottom left
                uvs[3] = uv + texel_size * float2(half_width_c, half_width_f) * float2(1, -1);  // bottom right
                
                float3 normal_samples[4];
                float depth_samples[4], luminance_samples[4];
                
                for (int i = 0; i < 4; i++) {
                    depth_samples[i] = SampleSceneDepth(uvs[i]);
                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
                    luminance_samples[i] = SampleSceneLuminance(uvs[i]);
                }

                float sceneDepth = SampleSceneDepth(uv);
                
                // Apply edge detection kernel on the samples to compute edges.
                float edge_depth = RobertsCross(depth_samples);
                float edge_normal = RobertsCross(normal_samples);
                float edge_luminance = RobertsCross(luminance_samples);
                
                // Threshold the edges (discontinuity must be above certain threshold to be counted as an edge). The sensitivities are hardcoded here.
                float depth_threshold = 0.00008f;
                float depth_threshold_2 = 0.000001f;
                float edge_depth_final = edge_depth > depth_threshold_2 ? 0.15 : 0;
                edge_depth_final = edge_depth > depth_threshold ? 0.5 : 0;
                
                float normal_threshold = 0.25f;
                float normal_threshold_2 = 0.00001f;
                float edge_normal_final;
                edge_normal_final = edge_normal > normal_threshold_2 ? 0.5 : 0;
                edge_normal_final = edge_normal > normal_threshold ? 1 : 0;
                
                float luminance_threshold = 0.01f;
                edge_luminance = edge_luminance > luminance_threshold ? 0.05 : 0;
                
                // Combine the edges from depth/normals/luminance using the max operator.
                float edge = max(edge_depth_final, max(edge_normal_final, edge_luminance));
                
                // Color the edge with a custom color.
                return lerp(saturate(_DepthAmplifier * half4(sceneDepth, sceneDepth, sceneDepth, 1)), _OutlineColor, edge) * (1-sphereMask) + sonarLight; 
            }
            ENDHLSL
        }
    }
}