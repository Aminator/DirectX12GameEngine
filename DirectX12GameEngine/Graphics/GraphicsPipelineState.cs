using System;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public sealed class PipelineState
    {
        public PipelineState(GraphicsDevice device, InputElement[] inputElements, RootSignature rootSignature, ShaderBytecode vertexShader, ShaderBytecode pixelShader, ShaderBytecode hullShader = default, ShaderBytecode domainShader = default, ShaderBytecode geometryShader = default, RasterizerStateDescription? rasterizerStateDescription = null)
        {
            if (device.Presenter is null) throw new InvalidOperationException("The presenter of the graphics device cannot be null.");

            RootSignature = rootSignature;

            RasterizerStateDescription rasterizerDescription = rasterizerStateDescription ?? RasterizerStateDescription.Default();
            rasterizerDescription.IsFrontCounterClockwise = true;

            DepthStencilStateDescription depthStencilStateDescription = DepthStencilStateDescription.Default();

            GraphicsPipelineStateDescription pipelineStateDescription = new GraphicsPipelineStateDescription
            {
                InputLayout = new InputLayoutDescription(inputElements),
                RootSignature = RootSignature,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                HullShader = hullShader,
                DomainShader = domainShader,
                GeometryShader = geometryShader,
                RasterizerState = rasterizerDescription,
                BlendState = BlendStateDescription.Default(),
                DepthStencilFormat = device.Presenter.PresentationParameters.DepthStencilFormat,
                DepthStencilState = depthStencilStateDescription,
                SampleMask = int.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                RenderTargetCount = 1,
                SampleDescription = new SampleDescription(1, 0),
                StreamOutput = new StreamOutputDescription()
            };

            pipelineStateDescription.RenderTargetFormats[0] = device.Presenter.PresentationParameters.BackBufferFormat;

            NativePipelineState = device.NativeDevice.CreateGraphicsPipelineState(pipelineStateDescription);
        }

        public PipelineState(GraphicsDevice device, RootSignature rootSignature, ShaderBytecode computeShader)
        {
            RootSignature = rootSignature;

            ComputePipelineStateDescription pipelineStateDescription = new ComputePipelineStateDescription
            {
                RootSignaturePointer = RootSignature,
                ComputeShader = computeShader
            };

            NativePipelineState = device.NativeDevice.CreateComputePipelineState(pipelineStateDescription);
        }

        public PrimitiveTopology PrimitiveTopology { get; } = PrimitiveTopology.TriangleList;

        public RootSignature RootSignature { get; }

        internal SharpDX.Direct3D12.PipelineState NativePipelineState { get; }
    }
}
