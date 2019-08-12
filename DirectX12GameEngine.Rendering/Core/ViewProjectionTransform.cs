using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public struct ViewProjectionTransform
    {
        [ShaderMember] public Matrix4x4 ViewMatrix { get; set; }

        [ShaderMember] public Matrix4x4 InverseViewMatrix { get; set; }

        [ShaderMember] public Matrix4x4 ProjectionMatrix { get; set; }

        [ShaderMember] public Matrix4x4 ViewProjectionMatrix { get; set; }
    }
}
