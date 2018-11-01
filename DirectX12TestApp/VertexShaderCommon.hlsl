cbuffer ViewProjectionConstantBuffer : register(b0)
{
    matrix ViewProjectionMatrices[2];
};

cbuffer WorldConstantBuffer : register(b1)
{
    matrix WorldMatrices[2];
};
