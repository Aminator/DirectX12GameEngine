using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class NormalStream
    {
        [NormalSemantic(0)]
        public static Vector3 Normal;

        [NormalSemantic(1)]
        public static Vector3 NormalWS;

        [TangentSemantic]
        public static Vector4 Tangent;

        public static Matrix4x4 TangentMatrix;

        public static Matrix4x4 TangentToWorld;
    }
}
