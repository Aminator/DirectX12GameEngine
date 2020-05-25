using System;
using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialDiffuseLambertModelFeature : IMaterialDiffuseModelFeature
    {
        public void Accept(ShaderGeneratorContext context)
        {
        }

        [ShaderMethod]
        public Vector3 ComputeDirectLightContribution(in MaterialShadingContext context)
        {
            Vector3 lightColorNDotL = context.LightColor * context.NDotL;

            Vector3 diffuseColor = context.DiffuseColor;
            diffuseColor *= Vector3.One - context.SpecularColor;

            return diffuseColor / (float)Math.PI * lightColorNDotL;
        }

        [ShaderMethod]
        public Vector3 ComputeEnvironmentLightContribution(in MaterialShadingContext context)
        {
            return default;
        }
    }
}
