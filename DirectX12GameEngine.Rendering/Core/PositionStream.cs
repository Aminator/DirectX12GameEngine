using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class PositionStream
    {
        [PositionSemantic]
        public static Vector4 Position;

        [PositionSemantic]
        public static Vector4 PositionWS;
    }
}
