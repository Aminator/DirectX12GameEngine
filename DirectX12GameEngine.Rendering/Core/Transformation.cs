using System.Numerics;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class Transformation
    {
        public static Matrix4x4 WorldMatrix { get; set; }

        public static Matrix4x4 ViewMatrix { get; set; }

        public static Matrix4x4 InverseViewMatrix { get; set; }

        public static Matrix4x4 ProjectionMatrix { get; set; }

        public static Matrix4x4 ViewProjectionMatrix { get; set; }
    }
}
