using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class ShaderBaseStream
    {
        [SystemPositionSemantic]
        public static Vector4 ShadingPosition;

        [SystemTargetSemantic]
        public static Vector4 ColorTarget;

        [SystemInstanceIdSemantic]
        public static uint InstanceId;

        [SystemRenderTargetArrayIndexSemantic]
        public static uint TargetId;

        [SystemIsFrontFaceSemantic]
        public static bool IsFrontFace;
    }
}
