using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class Transformation
    {
        [ShaderMember] public static Matrix4x4 WorldMatrix { get; set; }

        [ShaderMember] public static Matrix4x4 ViewMatrix { get; set; }

        [ShaderMember] public static Matrix4x4 InverseViewMatrix { get; set; }

        [ShaderMember] public static Matrix4x4 ProjectionMatrix { get; set; }

        [ShaderMember] public static Matrix4x4 ViewProjectionMatrix { get; set; }
    }
}
