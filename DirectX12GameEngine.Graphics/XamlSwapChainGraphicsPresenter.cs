using SharpGen.Runtime;
using System.Numerics;
using Vortice.DirectX;
using Vortice.DirectX.DXGI;
using Windows.UI.Xaml.Controls;

namespace DirectX12GameEngine.Graphics
{
    public class XamlSwapChainGraphicsPresenter : SwapChainGraphicsPresenter
    {
        private readonly SwapChainPanel swapChainPanel;

        public XamlSwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters, SwapChainPanel swapChainPanel)
            : base(device, presentationParameters, CreateSwapChain(device, presentationParameters, swapChainPanel))
        {
            this.swapChainPanel = swapChainPanel;
        }

        protected override void ResizeBackBuffer(int width, int height)
        {
            MatrixTransform = new Matrix3x2
            {
                M11 = 1.0f / swapChainPanel.CompositionScaleX,
                M22 = 1.0f / swapChainPanel.CompositionScaleY
            };

            base.ResizeBackBuffer(width, height);
        }

        private static IDXGISwapChain3 CreateSwapChain(GraphicsDevice device, PresentationParameters presentationParameters, SwapChainPanel swapChainPanel)
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

            swapChainDescription.AlphaMode = AlphaMode.Premultiplied;

            DXGI.CreateDXGIFactory2(false, out IDXGIFactory2 factory);
            Direct3DInterop.ISwapChainPanelNative nativePanel = (Direct3DInterop.ISwapChainPanelNative)swapChainPanel;
            using IDXGISwapChain1 tempSwapChain = factory.CreateSwapChainForComposition(device.NativeDirectCommandQueue, swapChainDescription);
            factory.Dispose();

            IDXGISwapChain3 swapChain = tempSwapChain.QueryInterface<IDXGISwapChain3>();
            nativePanel.SetSwapChain(swapChain.NativePointer);

            swapChain.MatrixTransform = new Matrix3x2
            {
                M11 = 1.0f / swapChainPanel.CompositionScaleX,
                M22 = 1.0f / swapChainPanel.CompositionScaleY
            };

            return swapChain;
        }
    }
}
