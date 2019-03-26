#if WINDOWS_UWP
using SharpDX;
using SharpDX.Direct3D12;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Holographic;
using Windows.Perception.Spatial;

namespace DirectX12GameEngine.Graphics
{
    public sealed class HolographicGraphicsPresenter : GraphicsPresenter
    {
        private const int BufferCount = 1;

        private Texture renderTarget;
        private SharpDX.Direct3D11.Resource direct3D11RenderTarget;

        public HolographicGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters, HolographicSpace holographicSpace)
            : base(device, presentationParameters)
        {
            if (GraphicsDevice.RenderTargetViewAllocator.DescriptorHeap.Description.DescriptorCount != BufferCount)
            {
                GraphicsDevice.RenderTargetViewAllocator.Dispose();
                GraphicsDevice.RenderTargetViewAllocator = new DescriptorAllocator(GraphicsDevice, DescriptorHeapType.RenderTargetView, descriptorCount: BufferCount);
            }

            using (SharpDX.DXGI.Device dxgiDevice = GraphicsDevice.NativeDirect3D11Device.QueryInterface<SharpDX.DXGI.Device>())
            {
                IDirect3DDevice direct3DInteropDevice = GraphicsDevice.CreateDirect3DDevice(dxgiDevice);

                HolographicSpace = holographicSpace;
                HolographicSpace.SetDirect3D11Device(direct3DInteropDevice);
            }

            HolographicDisplay = HolographicDisplay.GetDefault();
            SpatialStationaryFrameOfReference = HolographicDisplay.SpatialLocator.CreateStationaryFrameOfReferenceAtCurrentLocation();

            HolographicFrame = HolographicSpace.CreateNextFrame();
            HolographicSurface = HolographicFrame.GetRenderingParameters(HolographicFrame.CurrentPrediction.CameraPoses[0]).Direct3D11BackBuffer;
            HolographicBackBuffer = GetHolographicBackBuffer();

            renderTarget = CreateRenderTarget();
            direct3D11RenderTarget = CreateDirect3D11RenderTarget();

            Resize(PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);
        }

        public override Texture BackBuffer => renderTarget;

        public override object NativePresenter => HolographicSpace;

        public int HolographicBufferCount => HolographicDisplay.IsStereo ? 2 : 1;

        public HolographicDisplay HolographicDisplay { get; }

        public HolographicSpace HolographicSpace { get; }

        public SpatialStationaryFrameOfReference SpatialStationaryFrameOfReference { get; }

        public HolographicFrame HolographicFrame { get; private set; }

        internal SharpDX.Direct3D11.Texture2D HolographicBackBuffer { get; set; }

        internal IDirect3DSurface HolographicSurface { get; set; }

        public override void BeginDraw(CommandList commandList)
        {
            HolographicFrame = HolographicSpace.CreateNextFrame();
            HolographicBackBuffer = GetHolographicBackBuffer();
        }

        public override void Dispose()
        {
            renderTarget.Dispose();
        }

        public override void Present()
        {
            GraphicsDevice.NativeDirect3D11Device.ImmediateContext.CopyResource(direct3D11RenderTarget, HolographicBackBuffer);
            HolographicFrame.PresentUsingCurrentPrediction();
        }

        protected unsafe override void ResizeBackBuffer(int width, int height)
        {
            using SharpDX.Direct3D11.Device11On12 device11On12 = GraphicsDevice.NativeDirect3D11Device.QueryInterface<SharpDX.Direct3D11.Device11On12>();
            device11On12.ReleaseWrappedResources(new[] { direct3D11RenderTarget }, 1);

            renderTarget.Dispose();

            renderTarget = CreateRenderTarget();
            direct3D11RenderTarget = CreateDirect3D11RenderTarget();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height)
        {
            DepthStencilBuffer.Dispose();
            DepthStencilBuffer = CreateDepthStencilBuffer(width, height);
        }

        private SharpDX.Direct3D11.Resource CreateDirect3D11RenderTarget()
        {
            using SharpDX.Direct3D11.Device11On12 device11On12 = GraphicsDevice.NativeDirect3D11Device.QueryInterface<SharpDX.Direct3D11.Device11On12>();

            device11On12.CreateWrappedResource(
                BackBuffer.NativeResource,
                new SharpDX.Direct3D11.D3D11ResourceFlags { BindFlags = (int)Direct3DBindings.RenderTarget },
                (int)ResourceStates.RenderTarget,
                (int)ResourceStates.Present,
                Utilities.GetGuidFromType(typeof(SharpDX.Direct3D11.Resource)),
                out SharpDX.Direct3D11.Resource direct3D11RenderTarget);

            return direct3D11RenderTarget;
        }

        private Texture CreateRenderTarget()
        {
            return Texture.New2D(
                GraphicsDevice,
                PresentationParameters.BackBufferFormat,
                PresentationParameters.BackBufferWidth,
                PresentationParameters.BackBufferHeight,
                DescriptorHeapType.RenderTargetView,
                resourceFlags: ResourceFlags.AllowRenderTarget,
                arraySize: (short)HolographicBufferCount,
                mipLevels: 1);
        }

        private SharpDX.Direct3D11.Texture2D GetHolographicBackBuffer()
        {
            HolographicSurface = HolographicFrame.GetRenderingParameters(HolographicFrame.CurrentPrediction.CameraPoses[0]).Direct3D11BackBuffer;
            SharpDX.Direct3D11.Texture2D d3DBackBuffer = new SharpDX.Direct3D11.Texture2D(GraphicsDevice.CreateDXGISurface(HolographicSurface).NativePointer);

            PresentationParameters.BackBufferFormat = d3DBackBuffer.Description.Format;
            PresentationParameters.BackBufferWidth = d3DBackBuffer.Description.Width;
            PresentationParameters.BackBufferHeight = d3DBackBuffer.Description.Height;

            return d3DBackBuffer;
        }
    }
}
#endif
