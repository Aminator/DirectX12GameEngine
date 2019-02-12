using System.Numerics;

namespace DirectX12GameEngine.Rendering.Shaders
{
    public abstract class ShaderBase
    {
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
            [NormalSemantic] public Vector3 Normal;
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
            return default;
        }

        [Shader("pixel")]
        public virtual PSOutput PSMain(PSInput input)
        {
            return default;
        }
    }
}
