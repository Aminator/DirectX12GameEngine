using System;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

namespace DirectX12GameEngine
{
    public sealed class GraphicsPipelineState
    {
        public GraphicsPipelineState(GraphicsDevice device, SharpDX.D3DCompiler.ShaderBytecode vertexShader, SharpDX.D3DCompiler.ShaderBytecode pixelShader, InputElement[] inputElements, RootSignature? rootSignature = null, RasterizerStateDescription? rasterizerStateDescription = null)
        {
            if (device.Presenter is null) throw new InvalidOperationException("The presenter of the graphics device cannot be null.");

            RootSignature = rootSignature ?? device.CreateRootSignature(vertexShader.GetPart(SharpDX.D3DCompiler.ShaderBytecodePart.RootSignature));

            RasterizerStateDescription rasterizerDescription = rasterizerStateDescription ?? RasterizerStateDescription.Default();
            rasterizerDescription.IsFrontCounterClockwise = true;

            DepthStencilStateDescription depthStencilStateDescription = DepthStencilStateDescription.Default();

            PipelineStateDescription = new GraphicsPipelineStateDescription
            {
                InputLayout = new InputLayoutDescription(inputElements),
                RootSignature = RootSignature,
                VertexShader = new ShaderBytecode(vertexShader),
                PixelShader = new ShaderBytecode(pixelShader),
                RasterizerState = rasterizerDescription,
                BlendState = BlendStateDescription.Default(),
                DepthStencilFormat = device.Presenter.PresentationParameters.DepthStencilFormat,
                DepthStencilState = depthStencilStateDescription,
                SampleMask = int.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                RenderTargetCount = 1,
                Flags = PipelineStateFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                StreamOutput = new StreamOutputDescription(),
            };

            PipelineStateDescription.RenderTargetFormats[0] = device.Presenter.PresentationParameters.BackBufferFormat;

            NativePipelineState = device.NativeDevice.CreateGraphicsPipelineState(PipelineStateDescription);
        }

        public GraphicsPipelineStateDescription PipelineStateDescription { get; }

        public PrimitiveTopology PrimitiveTopology { get; } = PrimitiveTopology.TriangleList;

        public RootSignature RootSignature { get; }

        internal PipelineState NativePipelineState { get; }
    }
}
