using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticShaderClass]
    public static class MaterialPixelStream
    {
        [ShaderResource] public static Vector3 MaterialNormal;

        [ShaderResource] public static float MaterialRoughness;

        [ShaderResource] public static Vector4 MaterialColorBase;

        [ShaderResource] public static Vector4 MaterialDiffuse;
        [ShaderResource] public static Vector3 MaterialSpecular;

        [ShaderResource] public static Vector3 ViewWS;

        [ShaderResource] public static Vector3 MaterialDiffuseVisible;
        [ShaderResource] public static Vector3 MaterialSpecularVisible;

        [ShaderResource] public static float NDotV;

        [ShaderResource] public static float AlphaRoughness;

        [ShaderMethod]
        public static void Reset()
        {
            MaterialNormal = Vector3.UnitZ;
            MaterialRoughness = default;
            MaterialDiffuse = default;
            MaterialSpecular = default;
            MaterialDiffuseVisible = default;
            MaterialSpecularVisible = default;
            AlphaRoughness = default;
        }
    }
}
