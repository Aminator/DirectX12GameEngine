using System.Numerics;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        protected const int BufferCount = 2;

        private readonly Texture[] renderTargets = new Texture[BufferCount];

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters, IDXGISwapChain3 swapChain)
            : base(device, presentationParameters)
        {
            SwapChain = swapChain;

            if (GraphicsDevice.RenderTargetViewAllocator.DescriptorHeap.Description.DescriptorCount != BufferCount)
            {
                GraphicsDevice.RenderTargetViewAllocator.Dispose();
                GraphicsDevice.RenderTargetViewAllocator = new DescriptorAllocator(GraphicsDevice, DescriptorHeapType.RenderTargetView, BufferCount);
            }

            CreateRenderTargets();
        }

        public override Texture BackBuffer => renderTargets[SwapChain.GetCurrentBackBufferIndex()];

        public Matrix3x2 MatrixTransform { get => SwapChain.MatrixTransform; set => SwapChain.MatrixTransform = value; }

        public override object NativePresenter => SwapChain;

        protected IDXGISwapChain3 SwapChain { get; }

        public override void Dispose()
        {
            SwapChain.Dispose();

            foreach (Texture renderTarget in renderTargets)
            {
                renderTarget.Dispose();
            }

            base.Dispose();
        }

        public override void Present()
        {
            SwapChain.Present(PresentationParameters.SyncInterval, PresentFlags.None, PresentationParameters.PresentParameters);
        }

        protected override void ResizeBackBuffer(int width, int height)
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargets[i].Dispose();
            }

            SwapChain.ResizeBuffers(BufferCount, width, height, Format.Unknown, SwapChainFlags.None);

            CreateRenderTargets();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height)
        {
            DepthStencilBuffer.Dispose();
            DepthStencilBuffer = CreateDepthStencilBuffer();
        }

        private void CreateRenderTargets()
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargets[i] = new Texture(GraphicsDevice).InitializeFrom(SwapChain.GetBuffer<ID3D12Resource>(i), PresentationParameters.BackBufferFormat.IsSRgb());
            }
        }
    }
}
