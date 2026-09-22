#ifndef VANILLA_SIMPLE_TOON_INCLUDED
#define VANILLA_SIMPLE_TOON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST, _BumpMap_ST, _EmissionMap_ST;
    half4 _BaseColor, _ShadeColor, _EmissionColor, _RimColor, _OutlineColor;
    float _Surface, _AlphaClip, _Cutoff, _Cull, _QueueOffset;
    float _BumpScale, _BandThreshold, _BandSoftness, _ShadowStrength, _AmbientStrength;
    float _RimStrength, _RimPower, _OutlineWidth, _SrcBlend, _DstBlend, _ZWrite;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

struct ToonAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ToonVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    half fog : TEXCOORD4;
    float4 shadowCoord : TEXCOORD5;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ToonVaryings ToonVertex(ToonAttributes input)
{
    ToonVaryings output = (ToonVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = half4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w * GetOddNegativeScale());
    output.uv = input.uv;
    output.fog = ComputeFogFactor(position.positionCS.z);
    output.shadowCoord = GetShadowCoord(position);
    return output;
}

half4 ToonBase(float2 uv)
{
    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uv, _BaseMap)) * _BaseColor;
    #if defined(_ALPHATEST_ON)
        clip(color.a - _Cutoff);
    #endif
    return color;
}

half3 ToonNormal(ToonVaryings input, half faceSign)
{
    half3 normal = normalize(input.normalWS);
    #if defined(_NORMALMAP)
        half3 tangent = normalize(input.tangentWS.xyz);
        half3 bitangent = cross(normal, tangent) * input.tangentWS.w;
        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(input.uv, _BumpMap)), _BumpScale);
        normal = normalize(TransformTangentToWorld(normalTS, half3x3(tangent, bitangent, normal)));
    #endif
    return normal * faceSign;
}

half ToonBand(half3 normal, Light light, half edgeWidth)
{
    half lighting = (dot(normal, light.direction) * 0.5h + 0.5h) * lerp(1.0h, light.shadowAttenuation, _ShadowStrength);
    // Derivative filtering keeps a sharp band from flickering at a distance.
    half softness = max(max(_BandSoftness, edgeWidth), 0.001h);
    return smoothstep(_BandThreshold - softness, _BandThreshold + softness, lighting);
}

half4 ToonFragment(ToonVaryings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half4 base = ToonBase(input.uv);
    half3 normal = ToonNormal(input, IS_FRONT_VFACE(frontFace, 1.0h, -1.0h));
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
        float4 shadowCoord = input.shadowCoord;
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #endif
    Light mainLight = GetMainLight(shadowCoord);
    // Evaluate derivatives before the varying Forward+ light loop.
    half edgeWidth = max(length(fwidth(normal)) * 0.5h, fwidth(mainLight.shadowAttenuation));
    half band = ToonBand(normal, mainLight, edgeWidth);
    half3 color = base.rgb * lerp(_ShadeColor.rgb, half3(1,1,1), band) * mainLight.color * mainLight.distanceAttenuation;
    color += base.rgb * max(0.0h, SampleSH(normal)) * _AmbientStrength;

    #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
        InputData inputData = (InputData)0;
        inputData.positionWS = input.positionWS;
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
        #if USE_FORWARD_PLUS
            UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
            {
                Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1,1,1,1));
                color += base.rgb * ToonBand(normal, light, edgeWidth) * light.color * light.distanceAttenuation;
            }
        #endif
        uint lightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(lightCount)
            Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1,1,1,1));
            color += base.rgb * ToonBand(normal, light, edgeWidth) * light.color * light.distanceAttenuation;
        LIGHT_LOOP_END
    #endif

    half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half rim = pow(1.0h - saturate(dot(normal, view)), _RimPower) * _RimStrength;
    color += _RimColor.rgb * rim * band * mainLight.color * mainLight.distanceAttenuation;
    #if defined(_EMISSION)
        color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(input.uv, _EmissionMap)).rgb * _EmissionColor.rgb;
    #endif
    return half4(MixFog(color, input.fog), _Surface > 0.5 ? base.a : 1.0h);
}

ToonVaryings ToonOutlineVertex(ToonAttributes input)
{
    ToonVaryings output = ToonVertex(input);
    float2 direction = mul((float3x3)UNITY_MATRIX_VP, output.normalWS).xy * _ScaledScreenParams.xy;
    direction /= max(length(direction), 0.0001);
    output.positionCS.xy += direction * (2.0 * _OutlineWidth / _ScaledScreenParams.xy) * output.positionCS.w;
    return output;
}

half4 ToonOutlineFragment(ToonVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(_OutlineWidth - 0.0001);
    clip(0.5 - _Surface); // Eyelashes and other transparent overlays need no hull.
    ToonBase(input.uv);
    return half4(MixFog(_OutlineColor.rgb, input.fog), 1);
}

float3 _LightDirection, _LightPosition;
ToonVaryings ToonShadowVertex(ToonAttributes input)
{
    ToonVaryings output = ToonVertex(input);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        float3 direction = normalize(_LightPosition - output.positionWS);
    #else
        float3 direction = _LightDirection;
    #endif
    output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(output.positionWS, output.normalWS, direction)));
    return output;
}

half4 ToonDepthFragment(ToonVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    clip(0.5 - _Surface);
    ToonBase(input.uv);
    return 0;
}

half4 ToonDepthNormalsFragment(ToonVaryings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(0.5 - _Surface);
    ToonBase(input.uv);
    half3 normal = ToonNormal(input, IS_FRONT_VFACE(frontFace, 1.0h, -1.0h));
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 oct = PackNormalOctQuadEncode(normal);
        return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0);
    #else
        return half4(normal, 0);
    #endif
}
#endif
