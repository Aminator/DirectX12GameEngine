using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class ShaderBaseStream
    {
        [ShaderMember] [SystemPositionSemantic] public static Vector4 ShadingPosition;

        [ShaderMember] [SystemTargetSemantic] public static Vector4 ColorTarget;

        [ShaderMember] [SystemInstanceIdSemantic] public static uint InstanceId;

        [ShaderMember] [SystemRenderTargetArrayIndexSemantic] public static uint TargetId;

        [ShaderMember] [SystemIsFrontFaceSemantic] public static bool IsFrontFace;
    }
}
