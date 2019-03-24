using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Shadows
{
    public static class ShadowStream
    {
        [ShaderResource] public static Vector3 ShadowColor;
        [ShaderResource] public static Vector3 Thickness;
    }
}
