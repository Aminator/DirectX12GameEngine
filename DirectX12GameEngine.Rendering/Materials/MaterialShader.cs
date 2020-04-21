using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialShader : RasterizationShaderBase, IComputeShader
    {
#nullable disable
        [ConstantBufferView]
        public readonly uint RenderTargetCount;

        [ConstantBufferView]
        public readonly GlobalBuffer Globals;

        [ConstantBufferView]
        public readonly ViewProjectionTransform[] ViewProjectionTransforms;

        [ConstantBufferView]
        public readonly Matrix4x4[] WorldMatrices;

        [ShaderMember]
        public readonly DirectionalLightGroup DirectionalLights;

        [ShaderMember]
        public readonly SamplerResource Sampler;
#nullable enable

        public virtual void Accept(ShaderGeneratorContext context)
        {
            context.RootParameters.Add(new RootParameter(new RootConstants(context.ConstantBufferViewRegisterCount++, 0, 1), ShaderVisibility.All));
            context.RootParameters.Add(new RootParameter(new RootDescriptorTable(new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, context.ConstantBufferViewRegisterCount++)), ShaderVisibility.All));
            context.RootParameters.Add(new RootParameter(new RootDescriptorTable(new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, context.ConstantBufferViewRegisterCount++)), ShaderVisibility.All));
            context.RootParameters.Add(new RootParameter(new RootDescriptorTable(new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, context.ConstantBufferViewRegisterCount++)), ShaderVisibility.All));
            context.RootParameters.Add(new RootParameter(new RootDescriptorTable(new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, context.ConstantBufferViewRegisterCount++)), ShaderVisibility.All));
            context.RootParameters.Add(new RootParameter(new RootDescriptorTable(new DescriptorRange(DescriptorRangeType.Sampler, 1, context.SamplerRegisterCount++)), ShaderVisibility.All));
        }

        [ShaderMethod]
        [Shader("vertex")]
        public override VSOutput VSMain(VSInput input)
        {
            uint actualId = input.InstanceId / RenderTargetCount;
            uint targetId = input.InstanceId % RenderTargetCount;

            Vector4 positionWS = Vector4.Transform(new Vector4(input.Position, 1.0f), WorldMatrices[actualId]);
            Vector4 shadingPosition = Vector4.Transform(positionWS, ViewProjectionTransforms[targetId].ViewProjectionMatrix);

            VSOutput output = new VSOutput();

            output.PositionWS = positionWS;
            output.ShadingPosition = shadingPosition;
            output.Normal = input.Normal;
            output.NormalWS = Vector3.TransformNormal(input.Normal, WorldMatrices[actualId]);
            output.Tangent = input.Tangent;
            output.TexCoord = input.TexCoord;
            output.InstanceId = input.InstanceId;
            output.TargetId = targetId;

            return output;
        }

        [ShaderMethod]
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

            uint actualId = input.InstanceId / RenderTargetCount;
            Transformation.WorldMatrix = WorldMatrices[actualId];

            ViewProjectionTransform viewProjectionTransform = ViewProjectionTransforms[input.TargetId];
            Matrix4x4 inverseViewMatrix = viewProjectionTransform.InverseViewMatrix;

            Transformation.ViewMatrix = viewProjectionTransform.ViewMatrix;
            Transformation.InverseViewMatrix = inverseViewMatrix;
            Transformation.ProjectionMatrix = viewProjectionTransform.ProjectionMatrix;
            Transformation.ViewProjectionMatrix = viewProjectionTransform.ViewProjectionMatrix;

            Vector3 eyePosition = inverseViewMatrix.Translation;
            Vector4 positionWS = input.PositionWS;
            Vector3 worldPosition = new Vector3(positionWS.X, positionWS.Y, positionWS.Z);
            MaterialPixelStream.ViewWS = Vector3.Normalize(eyePosition - worldPosition);

            return output;
        }
    }
}
