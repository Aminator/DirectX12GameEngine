using SharpDX.DXGI;
using System;

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

        private static SwapChain3 CreateSwapChain(GraphicsDevice device, PresentationParameters presentationParameters, IntPtr windowHandle)
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

            using Factory4 factory = new Factory4();
            using SwapChain1 tempSwapChain = new SwapChain1(factory, device.NativeDirectCommandQueue, windowHandle, ref swapChainDescription);

            return tempSwapChain.QueryInterface<SwapChain3>();
        }
    }
}
