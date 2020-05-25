using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialMetalnessMapFeature : IMaterialSpecularFeature
    {
        public MaterialMetalnessMapFeature()
        {
        }

        public MaterialMetalnessMapFeature(IScalarShader metalnessMap)
        {
            MetalnessMap = metalnessMap;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            MetalnessMap.Accept(context);
        }

        public IScalarShader MetalnessMap { get; set; } = new ScalarShader();

        [ShaderMethod]
        public Vector3 ComputeSpecularColor(in SamplingContext context, ref Vector3 diffuseColor)
        {
            float metalness = MetalnessMap.ComputeScalar(context);

            Vector3 previousDiffuseColor = diffuseColor;
            diffuseColor = Vector3.Lerp(diffuseColor, Vector3.Zero, metalness);

            return Vector3.Lerp(new Vector3(0.02f, 0.02f, 0.02f), previousDiffuseColor, metalness);
        }
    }
}
