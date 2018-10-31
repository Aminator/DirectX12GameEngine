#define RS "RootFlags(ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT), DescriptorTable(CBV(b0)), DescriptorTable(CBV(b1)), DescriptorTable(SRV(t0)), StaticSampler(s0, filter = FILTER_MIN_LINEAR_MAG_MIP_POINT)"

#include "VertexShaderCommon.hlsl"

SamplerState Sampler : register(s0);

Texture2D Texture : register(t0);

struct VSInput
{
    float3 Position : Position;
    float3 Normal : Normal;
    float2 TexCoord : TexCoord;

    uint InstanceId : SV_InstanceId;
};

struct PSInput
{
    float4 Position : SV_Position;
    float2 TexCoord : TexCoord;

    uint TargetId : SV_RenderTargetArrayIndex;
};

[RootSignature(RS)]
PSInput VSMain(VSInput input)
{
    float4 position = float4(input.Position, 1.0f);
    position = mul(position, WorldMatrix);
    position = mul(position, ViewProjectionMatrix[input.InstanceId]);

    PSInput output;

    output.Position = position;
    output.TexCoord = input.TexCoord;

    output.TargetId = input.InstanceId;

    return output;
}

[RootSignature(RS)]
float4 PSMain(PSInput input) : SV_Target
{
    return Texture.Sample(Sampler, input.TexCoord);
}
