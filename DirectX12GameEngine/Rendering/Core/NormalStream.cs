using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    [StaticShaderClass]
    public static class NormalStream
    {
        [ShaderResource] [NormalSemantic(0)] public static Vector3 Normal;

        [ShaderResource] [NormalSemantic(1)] public static Vector3 NormalWS;
    }
}
