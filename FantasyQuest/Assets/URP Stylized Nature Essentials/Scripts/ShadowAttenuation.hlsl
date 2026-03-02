void MainLight_half(float3 WorldPos, out float3 Direction, out float3 Color, out float DistanceAtten, out float ShadowAtten)
{
    // Default initialization for all output parameters
    Direction = float3(0, 0, 0);
    Color = float3(1, 1, 1);
    DistanceAtten = 1.0;
    ShadowAtten = 1.0;

#if SHADERGRAPH_PREVIEW
    // Keep outputs initialized for preview mode
#else
    Light mainLight = GetMainLight();
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;

    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
    half shadowStrength = GetMainLightShadowStrength();
    ShadowAtten = SampleShadowmap(
        shadowCoord,
        TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture),
        shadowSamplingData,
        shadowStrength,
        false
    );
#endif
}