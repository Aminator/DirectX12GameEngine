using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Shadows
{
    [StaticShaderClass]
    public static class ShadowStream
    {
        [ShaderResource] public static Vector3 ShadowColor;
        [ShaderResource] public static Vector3 Thickness;
    }
}
