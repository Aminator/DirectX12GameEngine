#define RS "RootFlags(ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT), DescriptorTable(CBV(b0)), DescriptorTable(CBV(b1)), DescriptorTable(CBV(b2))"

#include "VertexShaderCommon.hlsl"

cbuffer ColorBuffer : register(b2)
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
    float2 TexCoord : TexCoord;

    uint TargetId : SV_RenderTargetArrayIndex;
};

[RootSignature(RS)]
PSInput VSMain(VSInput input)
{
    float4 position = float4(input.Position, 1.0f);
    position = mul(position, WorldMatrices[input.InstanceId]);

    uint targetId = input.InstanceId % 2;
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
