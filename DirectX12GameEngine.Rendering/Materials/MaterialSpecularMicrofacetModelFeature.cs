using System.Numerics;
using DirectX12GameEngine.Rendering.Lights;
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
        public Vector3 ComputeDirectLightContribution()
        {
            Vector3 specularColor = MaterialPixelStream.MaterialSpecularVisible;

            Vector3 fresnel = Fresnel.Compute(specularColor);
            float visibility = Visibility.Compute();
            float normalDistribution = NormalDistribution.Compute();

            Vector3 reflected = fresnel * visibility * normalDistribution / 4.0f;

            return reflected * LightStream.LightSpecularColorNDotL;
        }

        [ShaderMethod]
        public Vector3 ComputeEnvironmentLightContribution()
        {
            return default;
        }
    }
}
