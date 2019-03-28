using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialAttributes : MaterialShader, IMaterialAttributes
    {
        public virtual void Visit(MaterialGeneratorContext context)
        {
            Surface.Visit(context);
            MicroSurface.Visit(context);
            Diffuse.Visit(context);
            DiffuseModel.Visit(context);
            Specular.Visit(context);
            SpecularModel.Visit(context);
        }

        #region Shader

        [ShaderResource] public IMaterialSurfaceFeature Surface { get; set; } = new MaterialNormalMapFeature();

        [ShaderResource] public IMaterialMicroSurfaceFeature MicroSurface { get; set; } = new MaterialRoughnessMapFeature();

        [ShaderResource] public IMaterialDiffuseFeature Diffuse { get; set; } = new MaterialDiffuseMapFeature();

        [ShaderResource] public IMaterialDiffuseModelFeature DiffuseModel { get; set; } = new MaterialDiffuseLambertModelFeature();

        [ShaderResource] public IMaterialSpecularFeature Specular { get; set; } = new MaterialMetalnessMapFeature();

        [ShaderResource] public IMaterialSpecularModelFeature SpecularModel { get; set; } = new MaterialSpecularMicrofacetModelFeature();

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

            Vector4 materialDiffuse = MaterialPixelStream.MaterialDiffuse;

            MaterialPixelShadingStream.ShadingColor += directLightingContribution * MathF.PI;
            MaterialPixelShadingStream.ShadingColorAlpha = materialDiffuse.W;
        }

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

        #endregion
    }
}
