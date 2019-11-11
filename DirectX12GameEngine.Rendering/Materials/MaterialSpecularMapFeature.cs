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

        public void Visit(MaterialGeneratorContext context)
        {
            SpecularMap.Visit(context);
        }

        public IComputeColor SpecularMap { get; set; } = new ComputeColor();

        [ShaderMember]
        [ShaderMethod]
        public void Compute()
        {
            Vector4 specular = SpecularMap.Compute();
            MaterialPixelStream.MaterialSpecular = new Vector3(specular.X, specular.Y, specular.Z);
        }
    }
}
