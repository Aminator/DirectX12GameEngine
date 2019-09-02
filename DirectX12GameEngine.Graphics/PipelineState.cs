using Vortice.Direct3D12;
using Vortice.DXGI;

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

        public PipelineState(GraphicsDevice device, InputElementDescription[] inputElements, ID3D12RootSignature rootSignature, byte[] vertexShader, byte[] pixelShader, byte[]? geometryShader = default, byte[]? hullShader = default, byte[]? domainShader = default)
            : this(device, CreateGraphicsPipelineStateDescription(device, inputElements, rootSignature, vertexShader, pixelShader, geometryShader, hullShader, domainShader))
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

        private static GraphicsPipelineStateDescription CreateGraphicsPipelineStateDescription(GraphicsDevice device, InputElementDescription[] inputElements, ID3D12RootSignature rootSignature, ShaderBytecode vertexShader, ShaderBytecode pixelShader, ShaderBytecode geometryShader, ShaderBytecode hullShader, ShaderBytecode domainShader)
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
                GeometryShader = geometryShader,
                HullShader = hullShader,
                DomainShader = domainShader,
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
                renderTargetFormats[i] = (Format)(device.CommandList.RenderTargets[i] as Texture).Description.Format;
            }

            pipelineStateDescription.RenderTargetFormats = renderTargetFormats;

            return pipelineStateDescription;
        }
    }
}
