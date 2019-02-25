using System.Numerics;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Rendering.Materials.Brdf;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialSpecularMicrofacetModelFeature : IMaterialSpecularModelFeature
    {
        public void Visit(MaterialGeneratorContext context)
        {
            Fresnel.Visit(context);
            Visibility.Visit(context);
            NormalDistribution.Visit(context);
        }

        #region Shader

        [ShaderResource] public IMaterialSpecularMicrofacetFresnelFunction Fresnel { get; set; } = new MaterialSpecularMicrofacetFresnelSchlick();

        [ShaderResource] public IMaterialSpecularMicrofacetVisibilityFunction Visibility { get; set; } = new MaterialSpecularMicrofacetVisibilitySmithSchlickGgx();

        [ShaderResource] public IMaterialSpecularMicrofacetNormalDistributionFunction NormalDistribution { get; set; } = new MaterialSpecularMicrofacetNormalDistributionGgx();

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

        #endregion
    }
}
