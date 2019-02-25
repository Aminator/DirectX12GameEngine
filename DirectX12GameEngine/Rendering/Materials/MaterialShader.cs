using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialShader : ShaderBase
    {
#nullable disable
        [ConstantBufferResource] public readonly uint RenderTargetCount;
        [ConstantBufferResource] public readonly ViewProjectionTransform[] ViewProjectionTransforms;
        [ConstantBufferResource] public Matrix4x4[] WorldMatrices;

        [ShaderResource] public readonly DirectionalLightGroup DirectionalLights;

        [ShaderResource] public readonly SamplerResource Sampler;
#nullable enable

        [Shader("vertex")]
        public override PSInput VSMain(VSInput input)
        {
            uint actualId = input.InstanceId / RenderTargetCount;
            uint targetId = input.InstanceId % RenderTargetCount;

            PSInput output;

            output.PositionWS = Vector4.Transform(new Vector4(input.Position, 1.0f), WorldMatrices[actualId]);
            output.ShadingPosition = Vector4.Transform(output.PositionWS, ViewProjectionTransforms[targetId].ViewProjectionMatrix);

            output.Normal = input.Normal;
            output.NormalWS = Vector3.TransformNormal(input.Normal, WorldMatrices[actualId]);
            output.Tangent = input.Tangent;
            output.TexCoord = input.TexCoord;

            output.InstanceId = input.InstanceId;
            output.TargetId = targetId;

            return output;
        }

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output = base.PSMain(input);

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
    }
}
