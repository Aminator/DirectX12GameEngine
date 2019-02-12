using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialAttributes : MaterialShader
    {
        public void Visit(MaterialGeneratorContext context)
        {
            Diffuse.Visit(context);
            DiffuseModel.Visit(context);
        }

        #region Shader

        [ShaderResource] public IComputeColor Diffuse { get; set; } = new ComputeColor(Vector4.Zero);

        [ShaderResource] public IMaterialSurfaceShading DiffuseModel { get; set; } = new MaterialSurfaceShadingDiffuseLambert();

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            Texturing.Sampler = Sampler;
            Texturing.TexCoord = input.TexCoord;
            NormalStream.Normal = input.Normal;

            Vector4 materialDiffuse = Diffuse.Compute();

            MaterialPixelStream.MaterialDiffuse = materialDiffuse;
            MaterialPixelStream.MaterialDiffuseVisible = new Vector3(materialDiffuse.X, materialDiffuse.Y, materialDiffuse.Z);

            Vector3 directLightingContribution = Vector3.Zero;

            for (int i = 0; i < DirectionalLights.LightCount; i++)
            {
                DirectionalLights.PrepareDirectLight(i);

                directLightingContribution += DiffuseModel.ComputeDirectLightContribution();
            }

            PSOutput output;
            output.Color = new Vector4(directLightingContribution, 1.0f);

            return output;
        }

        #endregion
    }
}
