using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialAttributes : MaterialShader, IMaterialAttributes
    {
        [ShaderResource] private readonly MaterialSurfaceLightingAndShading lightingAndShading;

        public MaterialAttributes()
        {
            lightingAndShading = new MaterialSurfaceLightingAndShading(DiffuseModel, SpecularModel);
        }

        public void Visit(MaterialGeneratorContext context)
        {
            lightingAndShading.Visit(context);

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

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output = base.PSMain(input);

            Surface.Compute();
            MicroSurface.Compute();
            Diffuse.Compute();
            Specular.Compute();

            lightingAndShading.Compute(DirectionalLights);

            ShaderBaseStream.ColorTarget = new Vector4(MaterialPixelShadingStream.ShadingColor, 1.0f/*MaterialPixelShadingStream.ShadingColorAlpha*/);
            output.ColorTarget = ShaderBaseStream.ColorTarget;

            return output;
        }

        #endregion
    }
}
