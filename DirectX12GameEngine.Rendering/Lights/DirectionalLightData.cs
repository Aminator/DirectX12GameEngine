using System.Numerics;

namespace DirectX12GameEngine.Rendering.Lights
{
    public struct DirectionalLightData
    {
        public Vector3 Color;
        private readonly float Padding0;
        public Vector3 Direction;
        private readonly float Padding1;
    }
}
