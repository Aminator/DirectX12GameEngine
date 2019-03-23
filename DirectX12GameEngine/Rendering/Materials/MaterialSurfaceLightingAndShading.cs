using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [StaticResource]
    public class MaterialSurfaceLightingAndShading : IComputeNode
    {
        [ShaderResource] private readonly IMaterialSurfaceShading diffuseModel;
        [ShaderResource] private readonly IMaterialSurfaceShading specularModel;

        public MaterialSurfaceLightingAndShading(IMaterialSurfaceShading diffuseModel, IMaterialSurfaceShading specularModel)
        {
            this.diffuseModel = diffuseModel;
            this.specularModel = specularModel;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            diffuseModel.Visit(context);
            specularModel.Visit(context);
        }

        [ShaderMethod]
        public void Compute(DirectionalLightGroup directionalLights)
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

            for (int i = 0; i < directionalLights.LightCount; i++)
            {
                directionalLights.PrepareDirectLight(i);
                MaterialPixelShadingStream.PrepareMaterialPerDirectLight();

                directLightingContribution += diffuseModel.ComputeDirectLightContribution();
                directLightingContribution += specularModel.ComputeDirectLightContribution();
            }

            Vector4 materialDiffuse = MaterialPixelStream.MaterialDiffuse;

            MaterialPixelShadingStream.ShadingColor += directLightingContribution * MathF.PI;
            MaterialPixelShadingStream.ShadingColorAlpha = materialDiffuse.W;
        }
    }
}
