cbuffer ViewProjectionConstantBuffer : register(b0)
{
    matrix ViewProjectionMatrix[2];
};

cbuffer WorldConstantBuffer : register(b1)
{
    matrix WorldMatrix;
};
