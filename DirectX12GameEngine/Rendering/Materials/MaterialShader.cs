using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialShader
    {
#nullable disable
        public readonly SamplerResource Sampler;

        [ConstantBufferResource] public readonly uint RenderTargetCount;
        [ConstantBufferResource] public readonly Matrix4x4[] ViewProjectionMatrices;
        [ConstantBufferResource] public readonly Matrix4x4[] WorldMatrices;
#nullable enable

        public struct VSInput
        {
            [PositionSemantic] public Vector3 Position;
            [NormalSemantic] public Vector3 Normal;
            [TextureCoordinateSemantic] public Vector2 TexCoord;

            [SystemInstanceIdSemantic] public uint InstanceId;
        }

        public struct PSInput
        {
            [SystemPositionSemantic] public Vector4 Position;
            [TextureCoordinateSemantic] public Vector2 TexCoord;

            [SystemRenderTargetArrayIndexSemantic] public uint TargetId;
        }

        public struct PSOutput
        {
            [SystemTargetSemantic] public Vector4 Color;
        }

        [Shader("vertex")]
        public virtual PSInput VSMain(VSInput input)
        {
            uint actualId = input.InstanceId / RenderTargetCount;
            uint targetId = input.InstanceId % RenderTargetCount;

            Vector4 position = new Vector4(input.Position, 1.0f);
            position = Vector4.Transform(position, WorldMatrices[actualId]);
            position = Vector4.Transform(position, ViewProjectionMatrices[targetId]);

            PSInput output;
            output.Position = position;
            output.TexCoord = input.TexCoord;
            output.TargetId = targetId;

            return output;
        }

        [Shader("pixel")]
        public virtual PSOutput PSMain(PSInput input)
        {
            PSOutput output;
            output.Color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

            return output;
        }
    }
}
