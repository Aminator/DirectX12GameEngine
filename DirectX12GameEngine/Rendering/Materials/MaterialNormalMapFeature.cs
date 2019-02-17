using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialNormalMapFeature : IMaterialSurfaceFeature
    {
        public MaterialNormalMapFeature()
        {
        }

        public MaterialNormalMapFeature(IComputeColor normalMap)
        {
            NormalMap = normalMap;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            NormalMap.Visit(context);
        }

        public static Vector4 DefaultNormalColor { get; } = new Vector4(0.5f, 0.5f, 1.0f, 1.0f);

        #region Shader

        [ShaderResource] public IComputeColor NormalMap { get; set; } = new ComputeColor(DefaultNormalColor);

        [ShaderMethod]
        public void Compute()
        {
            Vector4 normal = NormalMap.Compute();
            normal = 2.0f * normal - Vector4.One;

            MaterialPixelStream.MaterialNormal = new Vector3(normal.X, normal.Y, normal.Z);
        }

        #endregion
    }
}
