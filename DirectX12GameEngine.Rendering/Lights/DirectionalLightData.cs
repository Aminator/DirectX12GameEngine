using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    public struct DirectionalLightData
    {
        [ShaderResource] public Vector3 Color { get; set; }
        private readonly int Padding0;
        [ShaderResource] public Vector3 Direction { get; set; }
        private readonly int Padding1;
    }
}
