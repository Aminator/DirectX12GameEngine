using System.Numerics;

namespace DirectX12GameEngine.Rendering.Shaders
{
    public abstract class ShaderBase
    {
        public struct VSInput
        {
            [ShaderResource] [PositionSemantic] public Vector3 Position;
            [ShaderResource] [NormalSemantic] public Vector3 Normal;
            [ShaderResource] [TextureCoordinateSemantic] public Vector2 TexCoord;

            [ShaderResource] [SystemInstanceIdSemantic] public uint InstanceId;
        }

        public struct PSInput
        {
            [ShaderResource] [SystemPositionSemantic] public Vector4 Position;
            [ShaderResource] [NormalSemantic] public Vector3 Normal;
            [ShaderResource] [TextureCoordinateSemantic] public Vector2 TexCoord;

            [ShaderResource] [SystemRenderTargetArrayIndexSemantic] public uint TargetId;
        }

        public struct PSOutput
        {
            [ShaderResource] [SystemTargetSemantic] public Vector4 Color;
        }

        [Shader("vertex")]
        public virtual PSInput VSMain(VSInput input)
        {
            return default;
        }

        [Shader("pixel")]
        public virtual PSOutput PSMain(PSInput input)
        {
            return default;
        }
    }
}
