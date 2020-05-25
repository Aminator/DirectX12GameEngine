using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    [StaticResource]
    public class MaterialSpecularMicrofacetVisibilitySmithSchlickGgx : IMaterialSpecularMicrofacetVisibilityFunction
    {
        public void Accept(ShaderGeneratorContext context)
        {
        }

        [ShaderMethod]
        public float Compute(in MaterialShadingContext context)
        {
            return BrdfMicrofacet.VisibilitySmithSchlickGgx(context.AlphaRoughness, context.NDotL, context.NDotV);
        }
    }
}
