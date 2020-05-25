using System;
using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials.CelShading
{
    [StaticResource]
    public class MaterialDiffuseCelShadingModelFeature : IMaterialDiffuseModelFeature
    {
        public void Accept(ShaderGeneratorContext context)
        {
            RampFunction.Accept(context);
        }

        public IMaterialCelShadingLightFunction RampFunction { get; set; } = new MaterialCelShadingLightDefault();

        [ShaderMethod]
        public Vector3 ComputeDirectLightContribution(in MaterialShadingContext context)
        {
            Vector3 lightColorNDotL = context.LightColor * RampFunction.Compute(context.NDotL);

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
