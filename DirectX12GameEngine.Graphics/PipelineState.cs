using Vortice.DirectX.Direct3D12;
using Vortice.DirectX.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public sealed class PipelineState
    {
        public PipelineState(GraphicsDevice device, ComputePipelineStateDescription pipelineStateDescription)
        {
            IsCompute = true;
            RootSignature = pipelineStateDescription.RootSignature;
            NativePipelineState = device.NativeDevice.CreateComputePipelineState(pipelineStateDescription);
        }

        public PipelineState(GraphicsDevice device, ID3D12RootSignature rootSignature, byte[] computeShader)
            : this(device, CreateComputePipelineStateDescription(rootSignature, computeShader))
        {
        }

        public PipelineState(GraphicsDevice device, GraphicsPipelineStateDescription pipelineStateDescription)
        {
            IsCompute = false;
            RootSignature = pipelineStateDescription.RootSignature;
            NativePipelineState = device.NativeDevice.CreateGraphicsPipelineState(pipelineStateDescription);
        }

        public PipelineState(GraphicsDevice device, InputElementDescription[] inputElements, ID3D12RootSignature rootSignature, byte[] vertexShader, byte[] pixelShader, byte[]? hullShader = default, byte[]? domainShader = default, byte[]? geometryShader = default)
            : this(device, CreateGraphicsPipelineStateDescription(device, inputElements, rootSignature, vertexShader, pixelShader, hullShader, domainShader, geometryShader))
        {
        }

        public ID3D12RootSignature RootSignature { get; }

        internal bool IsCompute { get; }

        internal ID3D12PipelineState NativePipelineState { get; }

        private static ComputePipelineStateDescription CreateComputePipelineStateDescription(ID3D12RootSignature rootSignature, ShaderBytecode computeShader)
        {
            return new ComputePipelineStateDescription
            {
                RootSignature = rootSignature,
                ComputeShader = computeShader
            };
        }

        private static GraphicsPipelineStateDescription CreateGraphicsPipelineStateDescription(GraphicsDevice device, InputElementDescription[] inputElements, ID3D12RootSignature rootSignature, ShaderBytecode vertexShader, ShaderBytecode pixelShader, ShaderBytecode hullShader, ShaderBytecode domainShader, ShaderBytecode geometryShader)
        {
            RasterizerDescription rasterizerDescription = RasterizerDescription.CullNone;
            rasterizerDescription.FrontCounterClockwise = true;

            BlendDescription blendDescription = BlendDescription.AlphaBlend;

            GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
            {
                InputLayout = new InputLayoutDescription(inputElements),
                RootSignature = rootSignature,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                HullShader = hullShader,
                DomainShader = domainShader,
                GeometryShader = geometryShader,
                RasterizerState = rasterizerDescription,
                BlendState = blendDescription,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                StreamOutput = new StreamOutputDescription()
            };

            Texture? depthStencilBuffer = device.CommandList.DepthStencilBuffer;

            if (depthStencilBuffer != null)
            {
                pipelineStateDescription.DepthStencilFormat = (Format)depthStencilBuffer.Description.Format;
            }

            Format[] renderTargetFormats = new Format[device.CommandList.RenderTargets.Length];

            for (int i = 0; i < renderTargetFormats.Length; i++)
            {
                renderTargetFormats[i] = (Format)device.CommandList.RenderTargets[i].Description.Format;
            }

            pipelineStateDescription.RenderTargetFormats = renderTargetFormats;

            return pipelineStateDescription;
        }
    }
}
