#define RS "RootFlags(ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT), RootConstants(num32BitConstants = 1, b0), DescriptorTable(CBV(b1)), DescriptorTable(CBV(b2)), DescriptorTable(CBV(b3))"

#include "VertexShaderCommon.hlsl"

cbuffer ColorBuffer : register(b3)
{
    float4 Color;
};

struct VSInput
{
    float3 Position : Position;
    float3 Normal : Normal;

    uint InstanceId : SV_InstanceId;
};

struct PSInput
{
    float4 Position : SV_Position;

    uint TargetId : SV_RenderTargetArrayIndex;
};

[RootSignature(RS)]
PSInput VSMain(VSInput input)
{
	uint actualId = input.InstanceId / renderTargetCount;
	uint targetId = input.InstanceId % renderTargetCount;

    float4 position = float4(input.Position, 1.0f);
    position = mul(position, WorldMatrices[actualId]);
    position = mul(position, ViewProjectionMatrices[targetId]);

    PSInput output;
    output.Position = position;
    output.TargetId = targetId;

    return output;
}

[RootSignature(RS)]
float4 PSMain(PSInput input) : SV_Target
{
    return Color;
}
