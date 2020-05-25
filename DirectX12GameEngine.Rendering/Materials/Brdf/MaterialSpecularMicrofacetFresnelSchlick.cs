using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    [StaticResource]
    public class MaterialSpecularMicrofacetFresnelSchlick : IMaterialSpecularMicrofacetFresnelFunction
    {
        public void Accept(ShaderGeneratorContext context)
        {
        }

        [ShaderMethod]
        public Vector3 Compute(in MaterialShadingContext context)
        {
            return BrdfMicrofacet.FresnelSchlick(context.SpecularColor, context.LDotH);
        }
    }
}
