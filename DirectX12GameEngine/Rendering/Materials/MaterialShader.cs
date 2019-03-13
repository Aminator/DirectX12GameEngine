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
        [ConstantBufferResource] public readonly GlobalBuffer Globals;
        [ConstantBufferResource] public readonly ViewProjectionTransform[] ViewProjectionTransforms;
        [ConstantBufferResource] public Matrix4x4[] WorldMatrices;

        [ShaderResource] public readonly DirectionalLightGroup DirectionalLights;

        [ShaderResource] public readonly SamplerResource Sampler;
#nullable enable

        [Shader("vertex")]
        public override VSOutput VSMain(VSInput input)
        {
            uint actualId = input.InstanceId / RenderTargetCount;
            uint targetId = input.InstanceId % RenderTargetCount;

            Vector4 positionWS = Vector4.Transform(new Vector4(input.Position, 1.0f), WorldMatrices[actualId]);
            Vector4 shadingPosition = Vector4.Transform(positionWS, ViewProjectionTransforms[targetId].ViewProjectionMatrix);

            VSOutput output = new VSOutput
            {
                PositionWS = positionWS,
                ShadingPosition = shadingPosition,

                Normal = input.Normal,
                NormalWS = Vector3.TransformNormal(input.Normal, WorldMatrices[actualId]),
                Tangent = input.Tangent,
                TexCoord = input.TexCoord,

                InstanceId = input.InstanceId,
                TargetId = targetId
            };

            return output;
        }

        [Shader("pixel")]
        public override PSOutput PSMain(PSInput input)
        {
            PSOutput output = base.PSMain(input);

            Global.ElapsedTime = Globals.ElapsedTime;
            Global.TotalTime = Globals.TotalTime;

            ShaderBaseStream.ShadingPosition = input.ShadingPosition;
            ShaderBaseStream.InstanceId = input.InstanceId;
            ShaderBaseStream.TargetId = input.TargetId;
            ShaderBaseStream.IsFrontFace = input.IsFrontFace;

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

            return output;
        }
    }
}
