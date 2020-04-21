using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialSpecularMapFeature : IMaterialSpecularFeature
    {
        public MaterialSpecularMapFeature()
        {
        }

        public MaterialSpecularMapFeature(IComputeColor specularMap)
        {
            SpecularMap = specularMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            SpecularMap.Accept(context);
        }

        public IComputeColor SpecularMap { get; set; } = new ComputeColor();

        [ShaderMethod]
        public void Compute()
        {
            Vector4 specular = SpecularMap.Compute();
            MaterialPixelStream.MaterialSpecular = new Vector3(specular.X, specular.Y, specular.Z);
        }
    }
}
