using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticShaderClass]
    public static class MaterialPixelStream
    {
        [StaticResource] public static Vector4 MaterialDiffuse;
        [StaticResource] public static Vector4 MaterialSpecular;

        [StaticResource] public static Vector3 MaterialDiffuseVisible;
        [StaticResource] public static Vector3 MaterialSpecularVisible;
    }
}
