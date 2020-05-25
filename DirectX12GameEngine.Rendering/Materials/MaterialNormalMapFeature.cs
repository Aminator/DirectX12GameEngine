using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialNormalMapFeature : IMaterialNormalFeature
    {
        public MaterialNormalMapFeature()
        {
        }

        public MaterialNormalMapFeature(IColorShader normalMap)
        {
            NormalMap = normalMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            NormalMap.Accept(context);
        }

        public static Vector4 DefaultNormalColor { get; } = new Vector4(0.5f, 0.5f, 1.0f, 1.0f);

        public IColorShader NormalMap { get; set; } = new ColorShader(DefaultNormalColor);

        [ShaderMethod]
        public Vector3 ComputeNormal(in SamplingContext context)
        {
            Vector4 normal = NormalMap.ComputeColor(context);
            normal = 2.0f * normal - Vector4.One;

            return new Vector3(normal.X, normal.Y, normal.Z);
        }
    }
}
