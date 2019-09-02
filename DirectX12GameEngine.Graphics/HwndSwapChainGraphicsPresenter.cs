using System;
using Vortice.DirectX;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class HwndSwapChainGraphicsPresenter : SwapChainGraphicsPresenter
    {
        private readonly IntPtr windowHandle;

        public HwndSwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters, IntPtr windowHandle)
            : base(device, presentationParameters, CreateSwapChain(device, presentationParameters, windowHandle))
        {
            this.windowHandle = windowHandle;
        }

        private static IDXGISwapChain3 CreateSwapChain(GraphicsDevice device, PresentationParameters presentationParameters, IntPtr windowHandle)
        {
            SwapChainDescription1 swapChainDescription = new SwapChainDescription1
            {
                Width = presentationParameters.BackBufferWidth,
                Height = presentationParameters.BackBufferHeight,
                SampleDescription = new SampleDescription(1, 0),
                Stereo = presentationParameters.Stereo,
                Usage = Usage.RenderTargetOutput,
                BufferCount = BufferCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Format = (Format)presentationParameters.BackBufferFormat,
                Flags = SwapChainFlags.None,
                AlphaMode = AlphaMode.Unspecified
            };

            DXGI.CreateDXGIFactory2(false, out IDXGIFactory2 factory);
            using IDXGISwapChain1 tempSwapChain = factory.CreateSwapChainForHwnd(device.NativeDirectCommandQueue, windowHandle, swapChainDescription);
            factory.Dispose();

            return tempSwapChain.QueryInterface<IDXGISwapChain3>();
        }
    }
}
