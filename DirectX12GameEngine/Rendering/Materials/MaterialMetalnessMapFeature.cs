using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialMetalnessMapFeature : IMaterialSpecularFeature
    {
        public MaterialMetalnessMapFeature()
        {
        }

        public MaterialMetalnessMapFeature(IComputeScalar metalnessMap)
        {
            MetalnessMap = metalnessMap;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            MetalnessMap.Visit(context);
        }

        #region Shader

        [ShaderResource] public IComputeScalar MetalnessMap { get; set; } = new ComputeTextureScalar();

        [ShaderMethod]
        public void Compute()
        {
            float metalness = MetalnessMap.Compute();

            Vector4 materialDiffuse = MaterialPixelStream.MaterialDiffuse;
            Vector3 materialDiffuseRgb = new Vector3(materialDiffuse.X, materialDiffuse.Y, materialDiffuse.Z);

            MaterialPixelStream.MaterialSpecular = Vector3.Lerp(new Vector3(0.02f, 0.02f, 0.02f), materialDiffuseRgb, metalness);
            MaterialPixelStream.MaterialDiffuse = new Vector4(Vector3.Lerp(materialDiffuseRgb, Vector3.Zero, metalness), materialDiffuse.W);
        }

        #endregion
    }
}
