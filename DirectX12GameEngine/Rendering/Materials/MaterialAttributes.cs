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
            Surface.Visit(context);
            MicroSurface.Visit(context);
            Specular.Visit(context);
            SpecularModel.Visit(context);
        }

        #region Shader

        [ShaderResource] public IMaterialDiffuseFeature Diffuse { get; set; } = new MaterialDiffuseMapFeature();

        [ShaderResource] public IMaterialDiffuseModelFeature DiffuseModel { get; set; } = new MaterialDiffuseLambertModelFeature();

        [ShaderResource] public IMaterialSurfaceFeature Surface { get; set; } = new MaterialNormalMapFeature();

        [ShaderResource] public IMaterialMicroSurfaceFeature MicroSurface { get; set; } = new MaterialRoughnessMapFeature();

        [ShaderResource] public IMaterialSpecularFeature Specular { get; set; } = new MaterialMetalnessMapFeature();

        [ShaderResource] public IMaterialSpecularModelFeature SpecularModel { get; set; } = new MaterialSpecularMicrofacetModelFeature();

        [ShaderMethod]
        public void UpdateNormalFromTangentSpace(Vector3 normalInTangetSpace)
        {
            Vector3 normal = Vector3.Normalize(NormalStream.Normal);

            Vector4 tangent4 = NormalStream.Tangent;
            Vector3 tangent = Vector3.Normalize(new Vector3(tangent4.X, tangent4.Y, tangent4.Z));

            Vector3 bitangent = Vector3.Cross(normal, tangent);

            Matrix4x4 tangentMatrix = new Matrix4x4(
                tangent.X, tangent.Y, tangent.Z, 0.0f,
                bitangent.X, bitangent.Y, bitangent.Z, 0.0f,
                normal.X, normal.Y, normal.Z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);

            uint actualId = ShaderBaseStream.InstanceId / RenderTargetCount;
            NormalStream.TangentToWorld = Matrix4x4.Multiply(tangentMatrix, WorldMatrices[actualId]);

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
            PSOutput output = SetPSInputs(input);

            Diffuse.Compute();
            Surface.Compute();
            MicroSurface.Compute();
            Specular.Compute();

            ComputeMaterialSurfaceLightingAndShading();

            ShaderBaseStream.ColorTarget = new Vector4(MaterialPixelShadingStream.ShadingColor, MaterialPixelShadingStream.ShadingColorAlpha);
            output.ColorTarget = ShaderBaseStream.ColorTarget;

            return output;
        }

        // TODO: Allow base class method calls. Like this: PSOutput output = base.PSMain(input);
        // TODO: Write every method declaration at the beginning so that order does not matter.
        [ShaderMethod(0)]
        private PSOutput SetPSInputs(PSInput input)
        {
            PSOutput output;

            ShaderBaseStream.ShadingPosition = input.ShadingPosition;
            ShaderBaseStream.InstanceId = input.InstanceId;
            ShaderBaseStream.TargetId = input.TargetId;

            Texturing.Sampler = Sampler;
            Texturing.TexCoord = input.TexCoord;

            NormalStream.Normal = Vector3.Normalize(input.Normal);
            NormalStream.NormalWS = Vector3.Normalize(input.NormalWS);
            NormalStream.Tangent = input.Tangent;

            PositionStream.PositionWS = input.PositionWS;

            Matrix4x4 inverseViewMatrix = ViewProjectionTransforms[input.TargetId].InverseViewMatrix;
            Vector3 eyePosition = inverseViewMatrix.Translation;
            Vector4 positionWS = input.PositionWS;
            Vector3 worldPosition = new Vector3(positionWS.X, positionWS.Y, positionWS.Z);
            MaterialPixelStream.ViewWS = Vector3.Normalize(eyePosition - worldPosition);

            // TODO: Remove this.
            Matrix4x4 dummy = inverseViewMatrix * 4;

            ShaderBaseStream.ColorTarget = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            output.ColorTarget = ShaderBaseStream.ColorTarget;

            return output;
        }

        #endregion
    }
}
