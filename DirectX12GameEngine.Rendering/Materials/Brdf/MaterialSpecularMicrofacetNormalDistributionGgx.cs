using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    [StaticResource]
    public class MaterialSpecularMicrofacetNormalDistributionGgx : IMaterialSpecularMicrofacetNormalDistributionFunction
    {
        public void Accept(ShaderGeneratorContext context)
        {
        }

        [ShaderMethod]
        public float Compute(in MaterialShadingContext context)
        {
            return BrdfMicrofacet.NormalDistributionGgx(context.AlphaRoughness, context.NDotH);
        }
    }
}
