using System.Numerics;
using System.Runtime.InteropServices;

namespace DirectX12GameEngine.Rendering.Lights
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct DirectionalLightData
    {
        [FieldOffset(0)]
        public Vector3 Color;

        [FieldOffset(16)]
        public Vector3 Direction;
    }
}
