using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Lights;
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
        public Vector3 ComputeDirectLightContribution()
        {
            Vector3 lightColorNDotL = LightStream.LightColor * RampFunction.Compute(LightStream.NDotL);

            Vector3 diffuseColor = MaterialPixelStream.MaterialDiffuseVisible;
            diffuseColor *= Vector3.One - MaterialPixelStream.MaterialSpecularVisible;

            return diffuseColor / (float)Math.PI * lightColorNDotL;
        }

        [ShaderMethod]
        public Vector3 ComputeEnvironmentLightContribution()
        {
            return default;
        }
    }
}
