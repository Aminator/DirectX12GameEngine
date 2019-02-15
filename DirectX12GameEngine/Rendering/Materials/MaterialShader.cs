using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialShader : ShaderBase
    {
#nullable disable
        [ConstantBufferResource] public readonly uint RenderTargetCount;
        [ConstantBufferResource] public readonly ViewProjectionTransform[] ViewProjectionTransforms;
        [ConstantBufferResource] public Matrix4x4[] WorldMatrices;

        [ShaderResource] public readonly DirectionalLightGroup DirectionalLights;

        [ShaderResource] public readonly SamplerResource Sampler;
#nullable enable

        [Shader("vertex")]
        public override PSInput VSMain(VSInput input)
        {
            uint actualId = input.InstanceId / RenderTargetCount;
            uint targetId = input.InstanceId % RenderTargetCount;

            Vector4 position = new Vector4(input.Position, 1.0f);
            position = Vector4.Transform(position, WorldMatrices[actualId]);
            position = Vector4.Transform(position, ViewProjectionTransforms[targetId].ViewProjectionMatrix);

            PSInput output;
            output.Position = position;
            output.Normal = Vector3.Normalize(Vector3.TransformNormal(input.Normal, WorldMatrices[actualId]));
            output.TexCoord = input.TexCoord;
            output.TargetId = targetId;

            return output;
        }

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output;
            output.Color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

            return output;
        }
    }
}
