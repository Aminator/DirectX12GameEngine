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
        public void UpdateNormalFromTangentSpace(Vector3 normalInTangetSpace)
        {
            Vector3 normal = Vector3.Normalize(NormalStream.Normal);

            Vector4 meshTangent = NormalStream.Tangent;
            Vector3 tangent = Vector3.Normalize(new Vector3(meshTangent.X, meshTangent.Y, meshTangent.Z));

            Vector3 bitangent = meshTangent.W * Vector3.Cross(normal, tangent);

            NormalStream.TangentMatrix = new Matrix4x4(
                tangent.X, tangent.Y, tangent.Z, 0.0f,
                bitangent.X, bitangent.Y, bitangent.Z, 0.0f,
                normal.X, normal.Y, normal.Z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);

            uint actualId = ShaderBaseStream.InstanceId / RenderTargetCount;
            NormalStream.TangentToWorld = Matrix4x4.Multiply(NormalStream.TangentMatrix, WorldMatrices[actualId]);
            NormalStream.NormalWS = Vector3.Normalize(Vector3.TransformNormal(normalInTangetSpace, NormalStream.TangentToWorld));
        }

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

        [ShaderMethod]
        public void ComputeMaterialSurfaceLightingAndShading()
        {
            Vector3 materialNormal = Vector3.Normalize(MaterialPixelStream.MaterialNormal);
            UpdateNormalFromTangentSpace(materialNormal);

            if (!ShaderBaseStream.IsFrontFace)
            {
                NormalStream.NormalWS = -NormalStream.NormalWS;
            }

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
        }

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output = base.PSMain(input);

            Surface.Compute();
            MicroSurface.Compute();
            Diffuse.Compute();
            Specular.Compute();

            ComputeMaterialSurfaceLightingAndShading();

            ShaderBaseStream.ColorTarget = new Vector4(MaterialPixelShadingStream.ShadingColor, MaterialPixelShadingStream.ShadingColorAlpha);
            output.ColorTarget = ShaderBaseStream.ColorTarget;

            return output;
        }

        #endregion
    }
}
