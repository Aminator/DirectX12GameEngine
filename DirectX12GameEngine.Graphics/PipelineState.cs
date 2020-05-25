using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public sealed class PipelineState : IDisposable
    {
        public PipelineState(GraphicsDevice device, RootSignature rootSignature, byte[] computeShader)
            : this(device, rootSignature, CreateComputePipelineStateDescription(rootSignature, computeShader))
        {
        }

        internal PipelineState(GraphicsDevice device, RootSignature rootSignature, ComputePipelineStateDescription pipelineStateDescription)
        {
            IsCompute = true;
            RootSignature = rootSignature;
            NativePipelineState = device.NativeDevice.CreateComputePipelineState(pipelineStateDescription);
        }

        public PipelineState(GraphicsDevice device, RootSignature rootSignature, InputElementDescription[] inputElements, byte[] vertexShader, byte[] pixelShader, byte[]? geometryShader = default, byte[]? hullShader = default, byte[]? domainShader = default)
            : this(device, rootSignature, CreateGraphicsPipelineStateDescription(device, rootSignature, inputElements, vertexShader, pixelShader, geometryShader, hullShader, domainShader))
        {
        }

        internal PipelineState(GraphicsDevice device, RootSignature rootSignature, GraphicsPipelineStateDescription pipelineStateDescription)
        {
            IsCompute = false;
            RootSignature = rootSignature;
            NativePipelineState = device.NativeDevice.CreateGraphicsPipelineState(pipelineStateDescription);
        }

        public RootSignature RootSignature { get; }

        public bool IsCompute { get; }

        internal ID3D12PipelineState NativePipelineState { get; }

        public void Dispose()
        {
            NativePipelineState.Dispose();
        }

        private static ComputePipelineStateDescription CreateComputePipelineStateDescription(RootSignature rootSignature, byte[] computeShader)
        {
            return new ComputePipelineStateDescription
            {
                RootSignature = rootSignature.NativeRootSignature,
                ComputeShader = computeShader
            };
        }

        private static GraphicsPipelineStateDescription CreateGraphicsPipelineStateDescription(GraphicsDevice device, RootSignature rootSignature, InputElementDescription[] inputElements, byte[] vertexShader, byte[] pixelShader, byte[]? geometryShader, byte[]? hullShader, byte[]? domainShader)
        {
            RasterizerDescription rasterizerDescription = RasterizerDescription.CullNone;
            rasterizerDescription.FrontCounterClockwise = true;

            BlendDescription blendDescription = BlendDescription.AlphaBlend;
            InputLayoutDescription inputLayoutDescription = new InputLayoutDescription(inputElements.Select(i => Unsafe.As<InputElementDescription, Vortice.Direct3D12.InputElementDescription>(ref i)).ToArray());

            GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
            {
                InputLayout = inputLayoutDescription,
                RootSignature = rootSignature.NativeRootSignature,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                GeometryShader = geometryShader,
                HullShader = hullShader,
                DomainShader = domainShader,
                RasterizerState = rasterizerDescription,
                BlendState = blendDescription,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                StreamOutput = new StreamOutputDescription()
            };

            DepthStencilView? depthStencilBuffer = device.CommandList.DepthStencilBuffer;

            if (depthStencilBuffer != null)
            {
                pipelineStateDescription.DepthStencilFormat = (Format)depthStencilBuffer.Resource.Description.Format;
            }

            Format[] renderTargetFormats = new Format[device.CommandList.RenderTargets.Length];

            for (int i = 0; i < renderTargetFormats.Length; i++)
            {
                renderTargetFormats[i] = (Format)device.CommandList.RenderTargets[i].Resource.Description.Format;
            }

            pipelineStateDescription.RenderTargetFormats = renderTargetFormats;

            return pipelineStateDescription;
        }
    }
}
