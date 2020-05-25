using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    [ShaderContract]
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

        [ShaderMember]
        public IMaterialNormalFeature Surface { get; set; } = new MaterialNormalMapFeature();

        [ShaderMember]
        public IMaterialRoughnessFeature MicroSurface { get; set; } = new MaterialRoughnessMapFeature();

        [ShaderMember]
        public IMaterialDiffuseFeature Diffuse { get; set; } = new MaterialDiffuseMapFeature();

        [ShaderMember]
        public IMaterialDiffuseModelFeature DiffuseModel { get; set; } = new MaterialDiffuseLambertModelFeature();

        [ShaderMember]
        public IMaterialSpecularFeature Specular { get; set; } = new MaterialMetalnessMapFeature();

        [ShaderMember]
        public IMaterialSpecularModelFeature SpecularModel { get; set; } = new MaterialSpecularMicrofacetModelFeature();

        [ShaderMethod]
        private static Matrix4x4 GetTangentToWorldMatrix(Matrix4x4 worldMatrix, Vector3 meshNormal, Vector4 meshTangent)
        {
            Vector3 normal = Vector3.Normalize(meshNormal);
            Vector3 tangent = Vector3.Normalize(new Vector3(meshTangent.X, meshTangent.Y, meshTangent.Z));
            Vector3 bitangent = meshTangent.W * Vector3.Cross(normal, tangent);

            Matrix4x4 tangentMatrix = new Matrix4x4(
                tangent.X, tangent.Y, tangent.Z, 0.0f,
                bitangent.X, bitangent.Y, bitangent.Z, 0.0f,
                normal.X, normal.Y, normal.Z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);

            return Matrix4x4.Multiply(tangentMatrix, worldMatrix);
        }

        [ShaderMethod]
        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output = base.PSMain(input);

            SamplingContext samplingContext;
            samplingContext.Sampler = Sampler;
            samplingContext.TextureCoordinate = input.TextureCoordinate;

            uint actualId = input.InstanceId / RenderTargetCount;
            Matrix4x4 worldMatrix = WorldMatrices[actualId];

            ViewProjectionTransform viewProjectionTransform = ViewProjectionTransforms[input.TargetId];
            Matrix4x4 inverseViewMatrix = viewProjectionTransform.InverseViewMatrix;

            Vector3 eyePosition = inverseViewMatrix.Translation;
            Vector4 positionWS = input.PositionWS;
            Vector3 worldPosition = new Vector3(positionWS.X, positionWS.Y, positionWS.Z);
            Vector3 viewWS = Vector3.Normalize(eyePosition - worldPosition);

            Vector3 materialNormal = Surface.ComputeNormal(samplingContext);
            float roughness = MicroSurface.ComputeRoughness(samplingContext);
            Vector4 diffuseColorWithAlpha = Diffuse.ComputeDiffuseColor(samplingContext);
            Vector3 diffuseColor = new Vector3(diffuseColorWithAlpha.X, diffuseColorWithAlpha.Y, diffuseColorWithAlpha.Z);
            Vector3 specularColor = Specular.ComputeSpecularColor(samplingContext, ref diffuseColor);

            Matrix4x4 tangentToWorldMatrix = GetTangentToWorldMatrix(worldMatrix, input.Normal, input.Tangent);
            Vector3 normalWS = Vector3.Normalize(Vector3.TransformNormal(materialNormal, tangentToWorldMatrix));

            if (!input.IsFrontFace)
            {
                normalWS = -normalWS;
            }

            Vector3 directLightingContribution = Vector3.Zero;

            for (int i = 0; i < DirectionalLights.LightCount; i++)
            {
                Vector3 lightColor = DirectionalLights.ComputeLightColor(i);
                Vector3 lightDirection = DirectionalLights.ComputeLightDirection(i);

                MaterialShadingContext context;
                context.H = Vector3.Normalize(viewWS + lightDirection);
                context.NDotL = Math.Max(Vector3.Dot(normalWS, lightDirection), 0.0001f);
                context.NDotV = Math.Max(Vector3.Dot(normalWS, viewWS), 0.0001f);
                context.NDotH = Vector3.Dot(normalWS, context.H);
                context.LDotH = Vector3.Dot(lightDirection, context.H);
                context.LightColor = lightColor;
                context.DiffuseColor = diffuseColor;
                context.SpecularColor = specularColor;
                context.AlphaRoughness = Math.Max(roughness * roughness, 0.001f);

                directLightingContribution += DiffuseModel.ComputeDirectLightContribution(context);
                directLightingContribution += SpecularModel.ComputeDirectLightContribution(context);
            }

            Vector3 shadingColor = Vector3.Zero;
            shadingColor += directLightingContribution * (float)Math.PI;

            output.ColorTarget = new Vector4(shadingColor, diffuseColorWithAlpha.W);

            return output;
        }
    }
}
