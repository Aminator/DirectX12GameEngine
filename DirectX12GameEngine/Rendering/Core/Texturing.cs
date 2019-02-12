using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    [StaticShaderClass]
    public static class Texturing
    {
#nullable disable
        [StaticResource(Override = true)] public static SamplerResource Sampler;

        [StaticResource] public static Vector2 TexCoord;
#nullable enable
    }
}
