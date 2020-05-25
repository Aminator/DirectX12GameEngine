using System.Numerics;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialShader : RasterizationShaderBase, IShader
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
        public readonly SamplerState Sampler;
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

            VSOutput output;
            output.PositionWS = positionWS;
            output.ShadingPosition = shadingPosition;
            output.Normal = input.Normal;
            output.NormalWS = Vector3.TransformNormal(input.Normal, WorldMatrices[actualId]);
            output.Tangent = input.Tangent;
            output.TextureCoordinate = input.TextureCoordinate;
            output.InstanceId = input.InstanceId;
            output.TargetId = targetId;

            return output;
        }
    }
}
