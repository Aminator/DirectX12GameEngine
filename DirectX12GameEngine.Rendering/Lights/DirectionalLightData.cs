using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    public struct DirectionalLightData
    {
        [ShaderMember] public Vector3 Color { get; set; }
        private readonly int Padding0;
        [ShaderMember] public Vector3 Direction { get; set; }
        private readonly int Padding1;
    }
}
