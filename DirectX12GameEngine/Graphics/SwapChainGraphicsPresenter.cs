using System;
using System.Drawing;
using DirectX12GameEngine.Games;
using SharpDX;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

using Resource = SharpDX.Direct3D12.Resource;

namespace DirectX12GameEngine.Graphics
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
#if WINDOWS_UWP
                case GameContextCoreWindow context:
                    Windows.UI.Core.CoreWindow coreWindow = context.Control;
                    Windows.Graphics.Display.DisplayInformation displayInformation = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
                    coreWindow.SizeChanged += (s, e) => SizeChanged?.Invoke(this, new SizeChangedEventArgs(new Size((int)e.Size.Width, (int)e.Size.Height), displayInformation.RawPixelsPerViewPixel));

                    using (Factory4 factory = new Factory4())
                    using (ComObject window = new ComObject(coreWindow))
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, window, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                    }
                    break;
                case GameContextXaml context:
                    Windows.UI.Xaml.Controls.SwapChainPanel swapChainPanel = context.Control;
                    swapChainDescription.AlphaMode = AlphaMode.Premultiplied;
                    swapChainPanel.SizeChanged += (s, e) => SizeChanged?.Invoke(this, new SizeChangedEventArgs(new Size((int)e.NewSize.Width, (int)e.NewSize.Height), swapChainPanel.CompositionScaleX));
                    swapChainPanel.CompositionScaleChanged += (s, e) => this.swapChain.MatrixTransform = GetInverseCompositionScale(s.CompositionScaleX, s.CompositionScaleY);

                    using (Factory4 factory = new Factory4())
                    using (ISwapChainPanelNative nativePanel = ComObject.As<ISwapChainPanelNative>(swapChainPanel))
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                        nativePanel.SwapChain = swapChain;

                        swapChain.MatrixTransform = GetInverseCompositionScale(swapChainPanel.CompositionScaleX, swapChainPanel.CompositionScaleY);
                    }
                    break;
#elif NETCOREAPP
                case GameContextWinForms context:
                    System.Windows.Forms.Control control = context.Control;
                    control.ClientSizeChanged += (s, e) => SizeChanged?.Invoke(this, new SizeChangedEventArgs(new Size(control.ClientSize.Width, control.ClientSize.Height), 1.0));

                    using (Factory4 factory = new Factory4())
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, control.Handle, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                    }
                    break;
#endif
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

        private static SharpDX.Mathematics.Interop.RawMatrix3x2 GetInverseCompositionScale(float compositionScaleX, float compositionScaleY)
        {
            return new SharpDX.Mathematics.Interop.RawMatrix3x2
            {
                M11 = 1.0f / compositionScaleX,
                M22 = 1.0f / compositionScaleY
            };
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
            PresentationParameters.BackBufferWidth = (int)(e.Size.Width * e.ResolutionScale);
            PresentationParameters.BackBufferHeight = (int)(e.Size.Height * e.ResolutionScale);
        }
    }
}
