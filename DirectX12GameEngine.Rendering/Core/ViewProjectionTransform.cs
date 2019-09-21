using System.Numerics;

namespace DirectX12GameEngine.Rendering.Core
{
    public struct ViewProjectionTransform
    {
        public Matrix4x4 ViewMatrix { get; set; }

        public Matrix4x4 InverseViewMatrix { get; set; }

        public Matrix4x4 ProjectionMatrix { get; set; }

        public Matrix4x4 ViewProjectionMatrix { get; set; }
    }
}
