using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class Transformation
    {
        [ShaderResource] public static Matrix4x4 WorldMatrix { get; set; }

        [ShaderResource] public static Matrix4x4 ViewMatrix { get; set; }

        [ShaderResource] public static Matrix4x4 InverseViewMatrix { get; set; }

        [ShaderResource] public static Matrix4x4 ProjectionMatrix { get; set; }

        [ShaderResource] public static Matrix4x4 ViewProjectionMatrix { get; set; }
    }
}
