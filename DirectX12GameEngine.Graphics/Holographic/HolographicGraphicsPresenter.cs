using Vortice.Direct3D11;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Holographic;
using Windows.Perception.Spatial;

namespace DirectX12GameEngine.Graphics.Holographic
{
    public sealed class HolographicGraphicsPresenter : GraphicsPresenter
    {
        private const int BufferCount = 1;

        private Texture renderTarget;
        private ID3D11Resource direct3D11RenderTarget;

        public HolographicGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters, HolographicSpace holographicSpace)
            : base(device, presentationParameters)
        {
            if (GraphicsDevice.RenderTargetViewAllocator.DescriptorHeap.Description.DescriptorCount != BufferCount)
            {
                GraphicsDevice.RenderTargetViewAllocator.Dispose();
                GraphicsDevice.RenderTargetViewAllocator = new DescriptorAllocator(GraphicsDevice, DescriptorHeapType.RenderTargetView, descriptorCount: BufferCount);
            }

            using (IDXGIDevice dxgiDevice = GraphicsDevice.NativeDirect3D11Device.QueryInterface<IDXGIDevice>())
            {
                IDirect3DDevice direct3DInteropDevice = Direct3DInterop.CreateDirect3DDevice(dxgiDevice);

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

            DepthStencilBuffer.Dispose();
            DepthStencilBuffer = CreateDepthStencilBuffer();
        }

        public override Texture BackBuffer => renderTarget;

        public override object NativePresenter => HolographicSpace;

        public int HolographicBufferCount => HolographicDisplay.IsStereo ? 2 : 1;

        public HolographicDisplay HolographicDisplay { get; }

        public HolographicSpace HolographicSpace { get; }

        public SpatialStationaryFrameOfReference SpatialStationaryFrameOfReference { get; }

        public HolographicFrame HolographicFrame { get; private set; }

        internal ID3D11Texture2D HolographicBackBuffer { get; set; }

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
            GraphicsDevice.NativeDirect3D11Device.ImmediateContext.CopyResource(HolographicBackBuffer, direct3D11RenderTarget);
            HolographicFrame.PresentUsingCurrentPrediction();
        }

        protected override void ResizeBackBuffer(int width, int height)
        {
            //using ID3D11On12Device device11On12 = GraphicsDevice.NativeDirect3D11Device.QueryInterface<ID3D11On12Device>();
            //device11On12.ReleaseWrappedResources(direct3D11RenderTarget);

            //renderTarget.Dispose();

            //renderTarget = CreateRenderTarget();
            //direct3D11RenderTarget = CreateDirect3D11RenderTarget();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height)
        {
            //DepthStencilBuffer.Dispose();
            //DepthStencilBuffer = CreateDepthStencilBuffer();
        }

        private ID3D11Resource CreateDirect3D11RenderTarget()
        {
            using ID3D11On12Device device11On12 = GraphicsDevice.NativeDirect3D11Device.QueryInterface<ID3D11On12Device>();

            return device11On12.CreateWrappedResource(
                BackBuffer.NativeResource,
                new Vortice.Direct3D11.ResourceFlags { BindFlags = (int)Direct3DBindings.RenderTarget },
                (int)ResourceStates.RenderTarget,
                (int)ResourceStates.Present);
        }

        private Texture CreateRenderTarget()
        {
            return Texture.Create2D(
                GraphicsDevice,
                PresentationParameters.BackBufferWidth,
                PresentationParameters.BackBufferHeight,
                PresentationParameters.BackBufferFormat,
                ResourceFlags.AllowRenderTarget,
                1,
                (short)HolographicBufferCount);
        }

        private ID3D11Texture2D GetHolographicBackBuffer()
        {
            HolographicSurface = HolographicFrame.GetRenderingParameters(HolographicFrame.CurrentPrediction.CameraPoses[0]).Direct3D11BackBuffer;
            using IDXGISurface surface = Direct3DInterop.CreateDXGISurface(HolographicSurface);

            ID3D11Texture2D direct3DBackBuffer = new ID3D11Texture2D(surface.NativePointer);

            PresentationParameters.BackBufferFormat = ((PixelFormat)direct3DBackBuffer.Description.Format).ToSRgb();
            PresentationParameters.BackBufferWidth = direct3DBackBuffer.Description.Width;
            PresentationParameters.BackBufferHeight = direct3DBackBuffer.Description.Height;

            return direct3DBackBuffer;
        }
    }
}
