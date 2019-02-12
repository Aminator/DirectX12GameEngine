using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    public struct DirectionalLightData
    {
        [ShaderResource] public Vector3 Color;
        private readonly int Padding0;
        [ShaderResource] public Vector3 Direction;
        private readonly int Padding1;
    }
}
