using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Shadows
{
    public static class ShadowStream
    {
        [ShaderMember] public static Vector3 ShadowColor;
        [ShaderMember] public static Vector3 Thickness;
    }
}
