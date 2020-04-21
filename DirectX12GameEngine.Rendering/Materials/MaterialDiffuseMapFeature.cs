using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialDiffuseMapFeature : IMaterialDiffuseFeature
    {
        public MaterialDiffuseMapFeature()
        {
        }

        public MaterialDiffuseMapFeature(IComputeColor diffuseMap)
        {
            DiffuseMap = diffuseMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            DiffuseMap.Accept(context);
        }

        public IComputeColor DiffuseMap { get; set; } = new ComputeColor();

        [ShaderMethod]
        public void Compute()
        {
            Vector4 colorBase = DiffuseMap.Compute();

            MaterialPixelStream.MaterialColorBase = colorBase;
            MaterialPixelStream.MaterialDiffuse = colorBase;
        }
    }
}
