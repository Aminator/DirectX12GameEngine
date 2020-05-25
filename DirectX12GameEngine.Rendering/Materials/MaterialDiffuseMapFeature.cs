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

        public MaterialDiffuseMapFeature(IColorShader diffuseMap)
        {
            DiffuseMap = diffuseMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            DiffuseMap.Accept(context);
        }

        public IColorShader DiffuseMap { get; set; } = new ColorShader();

        [ShaderMethod]
        public Vector4 ComputeDiffuseColor(in SamplingContext context)
        {
            return DiffuseMap.ComputeColor(context);
        }
    }
}
