// Copyright 2024 Alejandro Villalba Avila

Shader "Game/S_BresenhamLineStripRenderer2D"
{
    Properties
    {
        [Toggle(IS_LIT)] _IsLit("Is Lit", Float) = 0
        [Toggle(USE_OVERLAY_TEXTURE)] _UseOverlayTexture("Use Overlay Texture Features", Float) = 0
        [Toggle(USE_DOTS)] _UseDots("Use Dotted Line Features", Float) = 0
        [Toggle(USE_GRADIENT)] _UseGradient("Use Color Gradient Features", Float) = 0
        [Toggle(USE_COLOR_PATTERN)] _UseColorPattern("Use Point Color Pattern Features", Float) = 0
        _LineColor("Line Color", Color) = (1, 0, 0, 1)
        _BackgroundColor("Background Color", Color) = (0, 0, 0, 0)
        _Thickness("Thickness", Float) = 4.0
        _DotOffset("Dot Offset", Float) = 0.0
        _DotLength("Dot Length", Float) = 1.0
        _DotSpaceLength("Dot Space Length", Float) = 0.0
        _GradientStartColor("Gradient Start Color", Color) = (0, 0, 0, 1)
        _GradientEndColor("Gradient End Color", Color) = (0, 0, 0, 1)
        _GradientLength("Gradient Length", Float) = 1.0
        _GradientOffset("Gradient Offset", Float) = 0.0
        _StartPoint("Start Point", Float) = 0.0
        _MaximumLength("Maximum Length", Float) = 1.0
        _StartPointAffectsAllOffsets("Start Point Affects All Offsets", Float) = 0.0
        _PointColorPattern("Point Color Pattern", 2D) = "white" {}
        _ColorOffset("Color Offset", Float) = 0.0
        _PointColorPatternCount("Point Color Pattern Length", Float) = 1.0
        _OverlayTexture("Overlay Texture", 2D) = "white" {}
        _OverlayTextureSize("Overlay Texture Size", Vector) = (0, 0, 0, 0)
        [HideInInspector] _StencilRef("Stencil Ref", Float) = 1.0
        [HideInInspector] _StencilComp("Stencil Comp", Float) = 8.0 // Always
        [HideInInspector] _StencilPass("Stencil Pass", Float) = 2.0 // Replace
    }

    HLSLINCLUDE
#if UNITY_VERSION < 60030000
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif
    ENDHLSL

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        BlendOp Add
        Blend 0 SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass [_StencilPass]
        }

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM

#if UNITY_VERSION >= 60030000
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
#endif

            #pragma vertex MainVertexShader
            #pragma fragment MainFragmentShader

#if UNITY_VERSION < 60030000
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
#endif

            #pragma shader_feature_local_fragment __ USE_OVERLAY_TEXTURE
            #pragma shader_feature_local_fragment __ USE_DOTS
            #pragma shader_feature_local_fragment __ USE_GRADIENT
            #pragma shader_feature_local_fragment __ USE_COLOR_PATTERN
            #pragma shader_feature_local __ IS_LIT
            #pragma shader_feature_local_fragment MAXIMUM_AMOUNT_OF_POINTS__2 MAXIMUM_AMOUNT_OF_POINTS__32 MAXIMUM_AMOUNT_OF_POINTS__128 MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED

#if !MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED
    // One of this keywords must be enabled to make it work
    #if MAXIMUM_AMOUNT_OF_POINTS__2
        #define MAXIMUM_AMOUNT_OF_POINTS 2
    #elif MAXIMUM_AMOUNT_OF_POINTS__32
        #define MAXIMUM_AMOUNT_OF_POINTS 32
    #elif MAXIMUM_AMOUNT_OF_POINTS__128
        #define MAXIMUM_AMOUNT_OF_POINTS 128
    #else
        #define MAXIMUM_AMOUNT_OF_POINTS 1 // This will never happen, included to prevement the compiler from failing
    #endif
#endif

            struct Attributes
            {
#if UNITY_VERSION < 60030000
                float3 positionOS   : POSITION;
#else
                COMMON_2D_INPUTS
#endif
                float4 color        : COLOR;
            };

            struct Varyings
            {
#if UNITY_VERSION < 60030000
                float4  positionCS  : SV_POSITION;
#else
    #if IS_LIT
                COMMON_2D_LIT_OUTPUTS
    #else
                COMMON_2D_OUTPUTS
    #endif
#endif
                float4  color       : COLOR0;
#if UNITY_VERSION < 60030000
    #if UNITY_VERSION >= 202120
        #if IS_LIT || SHADER_API_METAL // In Mac, it requires this field in any case
                half2   lightingUV  : TEXCOORD1;
        #endif
    #endif
#endif
                float4	originScreenPos : COLOR1;
                float   lineIndex   : POSITION1; // SV_PrimitiveID could not be used instead due to a bug in Unity and Metal in some versions
                float2	screenPos   : TEXCOORD2;
            };

#if UNITY_VERSION < 60030000
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
#else
    #if IS_LIT
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
    #else
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
    #endif
#endif

#if MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED
            TEXTURE2D(_PackedPoints);
#endif
#if USE_COLOR_PATTERN
            TEXTURE2D(_PointColorPattern);
#endif
#if USE_OVERLAY_TEXTURE
            TEXTURE2D(_OverlayTexture);
            SAMPLER(sampler_OverlayTexture);
#endif

#if UNITY_VERSION < 60030000
    #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
    #endif

    #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
    #endif

    #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
    #endif

    #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
    #endif
#endif

            CBUFFER_START(UnityPerMaterial)
                float4 _LineColor;
                float4 _BackgroundColor;
                float _Thickness;
                float _MaximumLength;
                float _StartPoint;
                float _StartPointAffectsAllOffsets;
#if USE_DOTS
                float _DotOffset;
                float _DotLength;
                float _DotSpaceLength;
#endif
#if USE_GRADIENT
                float4 _GradientStartColor;
                float4 _GradientEndColor;
                float _GradientLength;
                float _GradientOffset;
#endif
#if USE_COLOR_PATTERN
                float _ColorOffset;
                float _PointColorPatternCount;
#endif
#if USE_OVERLAY_TEXTURE
                float4 _OverlayTexture_ST;
                float2 _OverlayTextureSize;
#endif
#if !MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED
                float4 _PackedPoints[MAXIMUM_AMOUNT_OF_POINTS];
#endif

            CBUFFER_END

            Varyings MainVertexShader(Attributes v)
            {
                Varyings o = (Varyings)0;

                o.positionCS = TransformObjectToHClip(float3(v.positionOS.x, v.positionOS.y, 0.0f));
                float4 clipVertex = o.positionCS / o.positionCS.w;
                o.screenPos = ComputeScreenPos(clipVertex).xy;
                o.color = v.color;
                o.originScreenPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(0.0f, 0.0f, 0.0f, 1.0f)));

#if UNITY_VERSION >= 202120
    #if IS_LIT
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
    #endif
#endif
                o.lineIndex = round(v.positionOS.z);
                return o;
            }

#if UNITY_VERSION < 60030000
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#endif
            #include "S_BresenhamLineStrip.hlsl"

            float4 MainFragmentShader(Varyings i) : SV_TARGET
            {
                float2 pointP = i.screenPos.xy * _ScreenParams.xy;

                bool isPixelInLine = false;
                float pixelIndex = -1;
                IsPixelInLine(i.lineIndex, _Thickness, pointP, i.originScreenPos.xy, _PackedPoints, isPixelInLine, pixelIndex);

#if USE_DOTS

                // Dotted line
                _DotOffset = _StartPointAffectsAllOffsets != 0.0f ? _DotOffset - _StartPoint
                                                                  : _DotOffset;
                float pixelIndexWithDotOffset = pixelIndex + _DotOffset;
                float dotLenthAndDotSpaceLength = _DotLength + _DotSpaceLength;
                float modDottedIndex = round(fmod(pixelIndexWithDotOffset, _DotLength + _DotSpaceLength));
                isPixelInLine = isPixelInLine && ((pixelIndexWithDotOffset >= 0.0f) ? modDottedIndex < _DotLength
                                                                                    : (fmod(dotLenthAndDotSpaceLength + modDottedIndex, dotLenthAndDotSpaceLength) < _DotLength));
#endif
                
                // Line bounds
                isPixelInLine = isPixelInLine && (pixelIndex >= _StartPoint) && (pixelIndex < (_StartPoint + _MaximumLength));

#if USE_COLOR_PATTERN

    #if USE_DOTS
                float modPatternColorIndex = round(fmod( floor(pixelIndexWithDotOffset / dotLenthAndDotSpaceLength) + _ColorOffset, _PointColorPatternCount));
    #else

                // Point color pattern
                _ColorOffset = _StartPointAffectsAllOffsets != 0.0f ? _ColorOffset - _StartPoint
                                                                    : _ColorOffset;

                float modPatternColorIndex = round(fmod(pixelIndex + _ColorOffset, _PointColorPatternCount));
    #endif
                int patternColorIndex = modPatternColorIndex >= 0.0f ? modPatternColorIndex
                                                                     : fmod(_PointColorPatternCount + modPatternColorIndex, _PointColorPatternCount);
                float4 patternColor = _PointColorPatternCount > 0.0f ? _PointColorPattern.Load(int3(patternColorIndex, 0, 0))
                                                                     : float4(1.0f, 1.0f, 1.0f, 1.0f);
#endif

#if USE_GRADIENT

                // Gradient color
                _GradientOffset = _StartPointAffectsAllOffsets != 0.0f ? _GradientOffset - _StartPoint
                                                                       : _GradientOffset;
                float4 gradientColor = _GradientLength > 0.0f ? lerp(_GradientStartColor, _GradientEndColor, saturate((pixelIndex + _GradientOffset) / _GradientLength))
                                                              : _GradientEndColor;

#endif

#if USE_OVERLAY_TEXTURE

                // Overlay color
                float2 overlayByScreen = _ScreenParams.xy / _OverlayTextureSize;
                float4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTexture, sampler_OverlayTexture, (i.screenPos.xy - i.originScreenPos.xy) * overlayByScreen * _OverlayTexture_ST.xy + _OverlayTexture_ST.zw);

#endif

                float4 lineColor = 
#if USE_GRADIENT
                    gradientColor * 
#endif

#if USE_COLOR_PATTERN
                    patternColor * 
#endif

#if USE_OVERLAY_TEXTURE
                    overlayColor * 
#endif
                    _LineColor;

                float4 finalColor = isPixelInLine ? lineColor 
                                                  : _BackgroundColor;

                clip(finalColor.a == 0.0f ? -1.0f 
                                          : 1.0f);

                // Lighting
#if UNITY_VERSION >= 202120

                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(finalColor.rgb, finalColor.a, surfaceData);
    #if IS_LIT
                InitializeInputData(i.screenPos.xy, i.lightingUV, inputData);
                finalColor = CombinedShapeLightShared(surfaceData, inputData);
    #else
                InitializeInputData(i.screenPos.xy, inputData);
    #endif

#else // UNITY_VERSION < 202120

    #if IS_LIT
                finalColor = CombinedShapeLightShared(finalColor, float4(1.0f, 1.0f, 1.0f, 1.0f), i.screenPos);
    #endif

#endif
                return finalColor;
            }
            ENDHLSL
        }
    }

    CustomEditor "SiliconHeart.Rendering.LineRenderer2DShaderUI"
}
