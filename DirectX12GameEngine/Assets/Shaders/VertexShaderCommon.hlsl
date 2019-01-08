cbuffer RootConstants : register(b0)
{
	uint renderTargetCount;
};

cbuffer ViewProjectionConstantBuffer : register(b1)
{
    matrix ViewProjectionMatrices[2];
};

cbuffer WorldConstantBuffer : register(b2)
{
    matrix WorldMatrices[2];
};
