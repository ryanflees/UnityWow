Shader "Vanilla/Simple Toon"
{
    Properties
    {
        [Header(Surface)]
        [Enum(Opaque,0,Transparent,1)] _Surface("Surface", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        _QueueOffset("Queue Offset", Range(-50,50)) = 0

        [Header(Base)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0,2)) = 1

        [Header(Toon Lighting)]
        _ShadeColor("Shadow Tint", Color) = (0.6,0.52,0.6,1)
        _BandThreshold("Light Band Threshold", Range(0.01,0.99)) = 0.55
        _BandSoftness("Light Band Softness", Range(0,0.3)) = 0.03
        _ShadowStrength("Received Shadow Strength", Range(0,1)) = 1
        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.35

        [Header(Rim)]
        [HDR] _RimColor("Rim Color", Color) = (1,0.9,0.8,1)
        _RimStrength("Rim Strength", Range(0,1)) = 0.08
        _RimPower("Rim Power", Range(1,12)) = 5

        [Header(Emission)]
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.06,0.04,0.05,1)
        _OutlineWidth("Outline Width in Pixels", Range(0,4)) = 1

        [HideInInspector] _SrcBlend("Source Blend", Float) = 1
        [HideInInspector] _DstBlend("Destination Blend", Float) = 0
        [HideInInspector] _ZWrite("Depth Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "UniversalMaterialType"="Lit" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "SimpleToon.hlsl"
        ENDHLSL

        // Default URP forward rendering draws this shell before the lit surface.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonOutlineVertex
            #pragma fragment ToonOutlineFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull [_Cull]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonShadowVertex
            #pragma fragment ToonDepthFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonVertex
            #pragma fragment ToonDepthFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonVertex
            #pragma fragment ToonDepthNormalsFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    CustomEditor "CR.SimpleToonShaderGUI"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
