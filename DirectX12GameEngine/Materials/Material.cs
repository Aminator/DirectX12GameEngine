using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

namespace DirectX12GameEngine
{
    public class Material
    {
#nullable disable
        public Material()
        {
        }
#nullable enable

        public Material(GraphicsDevice device, MaterialAttributes attributes)
        {
            GraphicsDevice = device;

            Attributes = attributes;
            Attributes.Visit(this);

            PipelineState = CreateGraphicsPipelineState();

            (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = CopyDescriptors();
        }

        public MaterialAttributes Attributes { get; set; }

        public GraphicsDevice GraphicsDevice { get; }

        public GraphicsPipelineState PipelineState { get; }

        public IList<Texture> Textures { get; } = new List<Texture>();

        internal CpuDescriptorHandle NativeCpuDescriptorHandle { get; }

        internal GpuDescriptorHandle NativeGpuDescriptorHandle { get; }

        private (CpuDescriptorHandle, GpuDescriptorHandle) CopyDescriptors()
        {
            int[] srcDescriptorRangeStarts = new int[Textures.Count];

            for (int i = 0; i < srcDescriptorRangeStarts.Length; i++)
            {
                srcDescriptorRangeStarts[i] = 1;
            }

            var (cpuDescriptorHandle, gpuDescriptorHandle) = GraphicsDevice.ShaderResourceViewAllocator.Allocate(Textures.Count);

            GraphicsDevice.NativeDevice.CopyDescriptors(
                1, new[] { cpuDescriptorHandle }, new[] { Textures.Count },
                Textures.Count, Textures.Select(t => t.NativeCpuDescriptorHandle).ToArray(), srcDescriptorRangeStarts,
                DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

            return (cpuDescriptorHandle, gpuDescriptorHandle);
        }

        private RootSignature CreateRootSignature()
        {
            RootSignatureDescription rootSignatureDescription = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout,
                new RootParameter[]
                {
                    new RootParameter(ShaderVisibility.All,
                        new RootConstants(0, 0, 1)),
                    new RootParameter(ShaderVisibility.All,
                        new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 1)),
                    new RootParameter(ShaderVisibility.All,
                        new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 2)),
                    new RootParameter(ShaderVisibility.All,
                        new DescriptorRange(DescriptorRangeType.ConstantBufferView, Textures.Count, 3)),
                    new RootParameter(ShaderVisibility.All,
                        new DescriptorRange(DescriptorRangeType.ShaderResourceView, Textures.Count, 0))
                },
                new StaticSamplerDescription[]
                {
                    new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
                    {
                        Filter = Filter.MinLinearMagMipPoint
                    }
                });

            return GraphicsDevice.CreateRootSignature(rootSignatureDescription);
        }

        private GraphicsPipelineState CreateGraphicsPipelineState()
        {
            InputElement[] inputElements = new[]
            {
                new InputElement("Position", 0, Format.R32G32B32_Float, 0),
                new InputElement("Normal", 0, Format.R32G32B32_Float, 1),
                new InputElement("TexCoord", 0, Format.R32G32_Float, 2)
            };

            ShaderGenerationResult result = ShaderLoader.GenerateShaderSource(Attributes);

            if (result.ShaderSource is null) throw new Exception("Shader source cannot be null.");

            (ShaderBytecode VertexShader, ShaderBytecode PixelShader, ShaderBytecode HullShader, ShaderBytecode DomainShader, ShaderBytecode GeometryShader) shaders = default;

            shaders.VertexShader = result.VertexShader is null ? throw new Exception("Vertex shader must be present.") : ShaderLoader.CompileShaderLegacy(result.ShaderSource, SharpDX.D3DCompiler.ShaderVersion.VertexShader, result.VertexShader.Name);
            shaders.PixelShader = result.PixelShader is null ? throw new Exception("Pixel shader must be present.") : ShaderLoader.CompileShaderLegacy(result.ShaderSource, SharpDX.D3DCompiler.ShaderVersion.PixelShader, result.PixelShader.Name);
            shaders.HullShader = result.HullShader is null ? default : ShaderLoader.CompileShader(result.ShaderSource, SharpDX.D3DCompiler.ShaderVersion.PixelShader, result.HullShader.Name);
            shaders.DomainShader = result.DomainShader is null ? default : ShaderLoader.CompileShader(result.ShaderSource, SharpDX.D3DCompiler.ShaderVersion.PixelShader, result.DomainShader.Name);
            shaders.GeometryShader = result.GeometryShader is null ? default : ShaderLoader.CompileShader(result.ShaderSource, SharpDX.D3DCompiler.ShaderVersion.PixelShader, result.GeometryShader.Name);

            RootSignature rootSignature = CreateRootSignature();

            return new GraphicsPipelineState(GraphicsDevice, inputElements, rootSignature,
                shaders.VertexShader, shaders.PixelShader, shaders.HullShader, shaders.DomainShader, shaders.GeometryShader);
        }
    }
}
