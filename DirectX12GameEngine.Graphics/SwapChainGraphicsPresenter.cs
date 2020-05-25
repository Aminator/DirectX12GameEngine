using System.Numerics;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        protected const int BufferCount = 2;

        private readonly RenderTargetView[] renderTargets = new RenderTargetView[BufferCount];

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

        public override RenderTargetView BackBuffer => renderTargets[SwapChain.GetCurrentBackBufferIndex()];

        public Matrix3x2 MatrixTransform { get => SwapChain.MatrixTransform; set => SwapChain.MatrixTransform = value; }

        public override object NativePresenter => SwapChain;

        protected IDXGISwapChain3 SwapChain { get; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SwapChain.Dispose();

                foreach (RenderTargetView renderTarget in renderTargets)
                {
                    renderTarget.Resource.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public override void Present()
        {
            SwapChain.Present(PresentationParameters.SyncInterval, PresentFlags.None, PresentationParameters.PresentParameters);
        }

        protected override void ResizeBackBuffer(int width, int height)
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargets[i].Resource.Dispose();
            }

            SwapChain.ResizeBuffers(BufferCount, width, height, Format.Unknown, SwapChainFlags.None);

            CreateRenderTargets();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height)
        {
            DepthStencilBuffer.Resource.Dispose();
            DepthStencilBuffer = CreateDepthStencilBuffer();
        }

        private void CreateRenderTargets()
        {
            for (int i = 0; i < BufferCount; i++)
            {
                Texture renderTargetTexture = new Texture(GraphicsDevice, SwapChain.GetBuffer<ID3D12Resource>(i));
                renderTargets[i] = RenderTargetView.FromTexture2D(renderTargetTexture, PresentationParameters.BackBufferFormat);
            }
        }
    }
}
