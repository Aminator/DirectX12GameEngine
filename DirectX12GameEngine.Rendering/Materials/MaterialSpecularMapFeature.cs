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

        public MaterialSpecularMapFeature(IColorShader specularMap)
        {
            SpecularMap = specularMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            SpecularMap.Accept(context);
        }

        public IColorShader SpecularMap { get; set; } = new ColorShader();

        [ShaderMethod]
        public Vector3 ComputeSpecularColor(in SamplingContext context, ref Vector3 diffuseColor)
        {
            Vector4 specular = SpecularMap.ComputeColor(context);
            return new Vector3(specular.X, specular.Y, specular.Z);
        }
    }
}
