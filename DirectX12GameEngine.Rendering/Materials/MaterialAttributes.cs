using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialAttributes : MaterialShader, IMaterialAttributes
    {
        public override void Accept(ShaderGeneratorContext context)
        {
            base.Accept(context);

            Surface.Accept(context);
            MicroSurface.Accept(context);
            Diffuse.Accept(context);
            DiffuseModel.Accept(context);
            Specular.Accept(context);
            SpecularModel.Accept(context);
        }

        public IMaterialSurfaceFeature Surface { get; set; } = new MaterialNormalMapFeature();

        public IMaterialMicroSurfaceFeature MicroSurface { get; set; } = new MaterialRoughnessMapFeature();

        public IMaterialDiffuseFeature Diffuse { get; set; } = new MaterialDiffuseMapFeature();

        public IMaterialDiffuseModelFeature DiffuseModel { get; set; } = new MaterialDiffuseLambertModelFeature();

        public IMaterialSpecularFeature Specular { get; set; } = new MaterialMetalnessMapFeature();

        public IMaterialSpecularModelFeature SpecularModel { get; set; } = new MaterialSpecularMicrofacetModelFeature();

        [ShaderMethod]
        public void ComputeSurfaceLightingAndShading()
        {
            Vector3 materialNormal = Vector3.Normalize(MaterialPixelStream.MaterialNormal);
            NormalUpdate.UpdateNormalFromTangentSpace(materialNormal);

            if (!ShaderBaseStream.IsFrontFace)
            {
                NormalStream.NormalWS = -NormalStream.NormalWS;
            }

            LightStream.Reset();
            MaterialPixelStream.PrepareMaterialForLightingAndShading();

            Vector3 directLightingContribution = Vector3.Zero;

            for (int i = 0; i < DirectionalLights.LightCount; i++)
            {
                DirectionalLights.PrepareDirectLight(i);
                MaterialPixelShadingStream.PrepareMaterialPerDirectLight();

                directLightingContribution += DiffuseModel.ComputeDirectLightContribution();
                directLightingContribution += SpecularModel.ComputeDirectLightContribution();
            }

            MaterialPixelShadingStream.ShadingColor += directLightingContribution * (float)Math.PI;
            MaterialPixelShadingStream.ShadingColorAlpha = MaterialPixelStream.MaterialDiffuse.W;
        }

        [ShaderMethod]
        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output = base.PSMain(input);

            Surface.Compute();
            MicroSurface.Compute();
            Diffuse.Compute();
            Specular.Compute();

            ComputeSurfaceLightingAndShading();

            ShaderBaseStream.ColorTarget = new Vector4(MaterialPixelShadingStream.ShadingColor, MaterialPixelShadingStream.ShadingColorAlpha);
            output.ColorTarget = ShaderBaseStream.ColorTarget;

            return output;
        }
    }
}
