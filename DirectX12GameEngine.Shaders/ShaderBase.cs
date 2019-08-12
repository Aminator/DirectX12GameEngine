using System.Numerics;

namespace DirectX12GameEngine.Shaders
{
    public abstract class ShaderBase
    {
        [ShaderMember]
        [Shader("vertex")]
        public virtual VSOutput VSMain(VSInput input)
        {
            return default;
        }

        [ShaderMember]
        [Shader("pixel")]
        public virtual PSOutput PSMain(PSInput input)
        {
            return default;
        }
    }

    public struct VSInput
    {
        [ShaderMember] [PositionSemantic] public Vector3 Position;
        [ShaderMember] [NormalSemantic] public Vector3 Normal;
        [ShaderMember] [TangentSemantic] public Vector4 Tangent;
        [ShaderMember] [TextureCoordinateSemantic] public Vector2 TexCoord;

        [ShaderMember] [SystemInstanceIdSemantic] public uint InstanceId;
    }

    public struct VSOutput
    {
        [ShaderMember] [PositionSemantic] public Vector4 PositionWS;
        [ShaderMember] [NormalSemantic(0)] public Vector3 Normal;
        [ShaderMember] [NormalSemantic(1)] public Vector3 NormalWS;
        [ShaderMember] [TangentSemantic] public Vector4 Tangent;
        [ShaderMember] [TextureCoordinateSemantic] public Vector2 TexCoord;

        [ShaderMember] [SystemPositionSemantic] public Vector4 ShadingPosition;
        [ShaderMember] [SystemInstanceIdSemantic] public uint InstanceId;
        [ShaderMember] [SystemRenderTargetArrayIndexSemantic] public uint TargetId;
    }

    public struct PSInput
    {
        [ShaderMember] [PositionSemantic] public Vector4 PositionWS;
        [ShaderMember] [NormalSemantic(0)] public Vector3 Normal;
        [ShaderMember] [NormalSemantic(1)] public Vector3 NormalWS;
        [ShaderMember] [TangentSemantic] public Vector4 Tangent;
        [ShaderMember] [TextureCoordinateSemantic] public Vector2 TexCoord;

        [ShaderMember] [SystemPositionSemantic] public Vector4 ShadingPosition;
        [ShaderMember] [SystemInstanceIdSemantic] public uint InstanceId;
        [ShaderMember] [SystemRenderTargetArrayIndexSemantic] public uint TargetId;

        [ShaderMember] [SystemIsFrontFaceSemantic] public bool IsFrontFace;
    }

    public struct PSOutput
    {
        [ShaderMember] [SystemTargetSemantic] public Vector4 ColorTarget;
    }
}
