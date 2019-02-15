using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    [StaticShaderClass]
    public static class PositionStream
    {
        [ShaderResource] [PositionSemantic] public static Vector3 Position;

        [ShaderResource] [PositionSemantic] public static Vector4 PositionWS;
    }
}
