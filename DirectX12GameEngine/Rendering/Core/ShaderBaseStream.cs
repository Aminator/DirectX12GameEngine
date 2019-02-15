using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    [StaticShaderClass]
    public static class ShaderBaseStream
    {
        [ShaderResource] [SystemPositionSemantic] public static Vector4 ShadingPosition;

        [ShaderResource] [SystemTargetSemantic] public static Vector4 ColorTarget;

        [ShaderResource] [SystemInstanceIdSemantic] public static uint InstanceId;

        [ShaderResource] [SystemRenderTargetArrayIndexSemantic] public static uint TargetId;
    }
}
