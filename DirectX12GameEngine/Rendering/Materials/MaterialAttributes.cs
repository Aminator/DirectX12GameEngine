using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialAttributes : MaterialShader, IMaterialAttributes
    {
        public void Visit(MaterialGeneratorContext context)
        {
            Diffuse.Visit(context);
            DiffuseModel.Visit(context);
            MicroSurface.Visit(context);
            Specular.Visit(context);
            SpecularModel.Visit(context);
        }

        #region Shader

        [ShaderResource] public IMaterialDiffuseFeature Diffuse { get; set; } = new MaterialDiffuseMapFeature();

        [ShaderResource] public IMaterialDiffuseModelFeature DiffuseModel { get; set; } = new MaterialDiffuseLambertModelFeature();

        [ShaderResource] public IMaterialMicroSurfaceFeature MicroSurface { get; set; } = new MaterialRoughnessMapFeature();

        [ShaderResource] public IMaterialSpecularFeature Specular { get; set; } = new MaterialMetalnessMapFeature();

        [ShaderResource] public IMaterialSpecularModelFeature SpecularModel { get; set; } = new MaterialSpecularMicrofacetModelFeature();

        [ShaderMethod]
        public static void PrepareMaterialForLightingAndShading()
        {
            Vector4 materialDiffuse = MaterialPixelStream.MaterialDiffuse;
            Vector3 materialSpecular = MaterialPixelStream.MaterialSpecular;

            MaterialPixelStream.MaterialDiffuseVisible = new Vector3(materialDiffuse.X, materialDiffuse.Y, materialDiffuse.Z);
            MaterialPixelStream.MaterialSpecularVisible = materialSpecular;

            MaterialPixelStream.NDotV = Math.Max(Vector3.Dot(NormalStream.NormalWS, MaterialPixelStream.ViewWS), 0.0001f);

            float roughness = MaterialPixelStream.MaterialRoughness;
            MaterialPixelStream.AlphaRoughness = Math.Max(roughness * roughness, 0.001f);
        }

        [ShaderMethod]
        public static void PrepareMaterialPerDirectLight()
        {
            MaterialPixelShadingStream.H = Vector3.Normalize(MaterialPixelStream.ViewWS + LightStream.LightDirectionWS);
            MaterialPixelShadingStream.NDotH = Vector3.Dot(NormalStream.NormalWS, MaterialPixelShadingStream.H);
            MaterialPixelShadingStream.LDotH = Vector3.Dot(LightStream.LightDirectionWS, MaterialPixelShadingStream.H);
            MaterialPixelShadingStream.VDotH = MaterialPixelShadingStream.LDotH;
        }

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            Texturing.Sampler = Sampler;
            Texturing.TexCoord = input.TexCoord;
            NormalStream.NormalWS = Vector3.Normalize(input.Normal);

            Matrix4x4 viewMatrix = ViewProjectionTransforms[input.TargetId].ViewMatrix;
            MaterialPixelStream.ViewWS = Vector3.Normalize(new Vector3(viewMatrix.M31, viewMatrix.M32, viewMatrix.M33));

            Diffuse.Compute();
            MicroSurface.Compute();
            Specular.Compute();

            LightStream.Reset();
            PrepareMaterialForLightingAndShading();

            Vector3 directLightingContribution = Vector3.Zero;

            for (int i = 0; i < DirectionalLights.LightCount; i++)
            {
                DirectionalLights.PrepareDirectLight(i);
                PrepareMaterialPerDirectLight();

                directLightingContribution += DiffuseModel.ComputeDirectLightContribution();
                directLightingContribution += SpecularModel.ComputeDirectLightContribution();
            }

            Vector4 materialDiffuse = MaterialPixelStream.MaterialDiffuse;

            MaterialPixelShadingStream.ShadingColor += directLightingContribution * (float)Math.PI;
            MaterialPixelShadingStream.ShadingColorAlpha = materialDiffuse.W;

            PSOutput output;
            output.Color = new Vector4(MaterialPixelShadingStream.ShadingColor, MaterialPixelShadingStream.ShadingColorAlpha);

            return output;
        }

        #endregion
    }
}
