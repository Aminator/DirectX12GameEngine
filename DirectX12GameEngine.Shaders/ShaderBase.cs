using System.Numerics;

namespace DirectX12GameEngine.Shaders
{
    public abstract class RasterizationShaderBase
    {
        [ShaderMember]
        [ShaderMethod]
        [Shader("vertex")]
        public virtual VSOutput VSMain(VSInput input)
        {
            return default;
        }

        [ShaderMember]
        [ShaderMethod]
        [Shader("pixel")]
        public virtual PSOutput PSMain(PSInput input)
        {
            return default;
        }
    }

    public abstract class ComputeShaderBase
    {
        [ShaderMember]
        [ShaderMethod]
        [Shader("compute")]
        public virtual void CSMain(CSInput input)
        {
        }
    }

    public struct CSInput
    {
        [SystemDispatchThreadIdSemantic]
        public Numerics.UInt3 DispatchThreadId;
    }

    public struct VSInput
    {
        [PositionSemantic]
        public Vector3 Position;

        [NormalSemantic]
        public Vector3 Normal;

        [TangentSemantic]
        public Vector4 Tangent;

        [TextureCoordinateSemantic]
        public Vector2 TexCoord;

        [SystemInstanceIdSemantic]
        public uint InstanceId;
    }

    public struct VSOutput
    {
        [PositionSemantic]
        public Vector4 PositionWS;

        [NormalSemantic(0)]
        public Vector3 Normal;

        [NormalSemantic(1)]
        public Vector3 NormalWS;

        [TangentSemantic]
        public Vector4 Tangent;

        [TextureCoordinateSemantic]
        public Vector2 TexCoord;

        [SystemPositionSemantic]
        public Vector4 ShadingPosition;

        [SystemInstanceIdSemantic]
        public uint InstanceId;

        [SystemRenderTargetArrayIndexSemantic]
        public uint TargetId;
    }

    public struct PSInput
    {
        [PositionSemantic]
        public Vector4 PositionWS;

        [NormalSemantic(0)]
        public Vector3 Normal;

        [NormalSemantic(1)]
        public Vector3 NormalWS;

        [TangentSemantic]
        public Vector4 Tangent;

        [TextureCoordinateSemantic]
        public Vector2 TexCoord;

        [SystemPositionSemantic]
        public Vector4 ShadingPosition;

        [SystemInstanceIdSemantic]
        public uint InstanceId;

        [SystemRenderTargetArrayIndexSemantic]
        public uint TargetId;

        [SystemIsFrontFaceSemantic]
        public bool IsFrontFace;
    }

    public struct PSOutput
    {
        [SystemTargetSemantic]
        public Vector4 ColorTarget;
    }
}
