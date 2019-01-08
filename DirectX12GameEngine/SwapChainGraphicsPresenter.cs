using System;
using SharpDX;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

using Resource = SharpDX.Direct3D12.Resource;

namespace DirectX12GameEngine
{
    public sealed class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private const int BufferCount = 2;

        private readonly Texture[] renderTargets = new Texture[BufferCount];

        private readonly SwapChain3 swapChain;

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            SizeChanged += OnSizeChanged;

            swapChain = CreateSwapChain();

            if (GraphicsDevice.RenderTargetViewAllocator.DescriptorHeap.Description.DescriptorCount != BufferCount)
            {
                GraphicsDevice.RenderTargetViewAllocator.Dispose();
                GraphicsDevice.RenderTargetViewAllocator = new DescriptorAllocator(GraphicsDevice, DescriptorHeapType.RenderTargetView, descriptorCount: BufferCount);
            }

            CreateRenderTargets();
        }

        public override event EventHandler<SizeChangedEventArgs> SizeChanged;

        public override Texture BackBuffer => renderTargets[swapChain.CurrentBackBufferIndex];

        public override object NativePresenter => swapChain;

        public override void Dispose()
        {
            swapChain.Dispose();

            foreach (Texture renderTarget in renderTargets)
            {
                renderTarget.Dispose();
            }

            base.Dispose();
        }

        private SwapChain3 CreateSwapChain()
        {
            SwapChainDescription1 swapChainDescription = new SwapChainDescription1
            {
                Width = PresentationParameters.BackBufferWidth,
                Height = PresentationParameters.BackBufferHeight,
                SampleDescription = new SampleDescription(1, 0),
                Stereo = PresentationParameters.Stereo,
                Usage = Usage.RenderTargetOutput,
                BufferCount = BufferCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Format = PresentationParameters.BackBufferFormat,
                Flags = SwapChainFlags.None,
                AlphaMode = AlphaMode.Unspecified
            };

            SwapChain3 swapChain;

            switch (PresentationParameters.GameContext)
            {
                case GameContextCoreWindow context:
                    CoreWindow coreWindow = context.Control;
                    coreWindow.SizeChanged += (s, e) => SizeChanged?.Invoke(this, new SizeChangedEventArgs(e.Size.Width, e.Size.Height));

                    using (Factory4 factory = new Factory4())
                    using (ComObject window = new ComObject(coreWindow))
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, window, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                    }
                    break;
                case GameContextXaml context:
                    SwapChainPanel swapChainPanel = context.Control;
                    swapChainDescription.AlphaMode = AlphaMode.Premultiplied;
                    swapChainPanel.SizeChanged += (s, e) => SizeChanged?.Invoke(this, new SizeChangedEventArgs(e.NewSize.Width, e.NewSize.Height));

                    using (Factory4 factory = new Factory4())
                    using (ISwapChainPanelNative nativePanel = ComObject.As<ISwapChainPanelNative>(swapChainPanel))
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                        nativePanel.SwapChain = swapChain;
                    }
                    break;
                default:
                    throw new NotSupportedException("This app context type is not supported while creating a swap chain.");
            }

            return swapChain;
        }

        public override void Present()
        {
            swapChain.Present(PresentationParameters.SyncInterval, PresentFlags.None, PresentationParameters.PresentParameters);
        }

        protected override void ResizeBackBuffer(int width, int height)
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargets[i].Dispose();
            }

            swapChain.ResizeBuffers(BufferCount, width, height, PresentationParameters.BackBufferFormat, SwapChainFlags.None);

            CreateRenderTargets();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height)
        {
            DepthStencilBuffer.Dispose();
            DepthStencilBuffer = CreateDepthStencilBuffer(width, height);
        }

        private void CreateRenderTargets()
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargets[i] = new Texture(GraphicsDevice, swapChain.GetBackBuffer<Resource>(i), DescriptorHeapType.RenderTargetView);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double resolutionScale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            PresentationParameters.BackBufferWidth = (int)(e.Width * resolutionScale);
            PresentationParameters.BackBufferHeight = (int)(e.Height * resolutionScale);
        }
    }
}
