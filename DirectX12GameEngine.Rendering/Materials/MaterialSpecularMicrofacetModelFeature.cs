using System.Numerics;
using DirectX12GameEngine.Rendering.Materials.Brdf;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialSpecularMicrofacetModelFeature : IMaterialSpecularModelFeature
    {
        public void Accept(ShaderGeneratorContext context)
        {
            Fresnel.Accept(context);
            Visibility.Accept(context);
            NormalDistribution.Accept(context);
        }

        public IMaterialSpecularMicrofacetFresnelFunction Fresnel { get; set; } = new MaterialSpecularMicrofacetFresnelSchlick();

        public IMaterialSpecularMicrofacetVisibilityFunction Visibility { get; set; } = new MaterialSpecularMicrofacetVisibilitySmithSchlickGgx();

        public IMaterialSpecularMicrofacetNormalDistributionFunction NormalDistribution { get; set; } = new MaterialSpecularMicrofacetNormalDistributionGgx();

        [ShaderMethod]
        public Vector3 ComputeDirectLightContribution(in MaterialShadingContext context)
        {
            Vector3 lightColorNDotL = context.LightColor * context.NDotL;

            Vector3 fresnel = Fresnel.Compute(context);
            float visibility = Visibility.Compute(context);
            float normalDistribution = NormalDistribution.Compute(context);

            Vector3 reflected = fresnel * visibility * normalDistribution / 4.0f;

            return reflected * lightColorNDotL;
        }

        [ShaderMethod]
        public Vector3 ComputeEnvironmentLightContribution(in MaterialShadingContext context)
        {
            return default;
        }
    }
}
