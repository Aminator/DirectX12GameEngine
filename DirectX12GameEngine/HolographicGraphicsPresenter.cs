using System;
using SharpDX;
using SharpDX.Direct3D12;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Holographic;
using Windows.Perception.Spatial;
using Windows.UI.Core;

namespace DirectX12GameEngine
{
    public sealed class HolographicGraphicsPresenter : GraphicsPresenter
    {
        private const int BufferCount = 2;

        private Texture renderTarget;
        private SharpDX.Direct3D11.Resource d3D11RenderTarget;

        public HolographicGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            SizeChanged += OnSizeChanged;

            if (GraphicsDevice.RenderTargetViewAllocator.DescriptorHeap.Description.DescriptorCount != BufferCount)
            {
                GraphicsDevice.RenderTargetViewAllocator.Dispose();
                GraphicsDevice.RenderTargetViewAllocator = new DescriptorAllocator(GraphicsDevice, DescriptorHeapType.RenderTargetView, descriptorCount: BufferCount);
            }

            switch (PresentationParameters.GameContext)
            {
                case GameContextHolographic context:
                    CoreWindow coreWindow = context.Control;
                    coreWindow.SizeChanged += (s, e) => SizeChanged?.Invoke(this, new SizeChangedEventArgs((int)e.Size.Width, (int)e.Size.Height));

                    using (SharpDX.DXGI.Device dxgiDevice = GraphicsDevice.NativeDirect3D11Device.QueryInterface<SharpDX.DXGI.Device>())
                    {
                        IDirect3DDevice d3DInteropDevice = GraphicsDevice.CreateDirect3DDevice(dxgiDevice);

                        HolographicSpace = context.HolographicSpace;
                        HolographicSpace.SetDirect3D11Device(d3DInteropDevice);
                    }

                    coreWindow.Activate();

                    HolographicDisplay = HolographicDisplay.GetDefault();
                    SpatialStationaryFrameOfReference = HolographicDisplay.SpatialLocator.CreateStationaryFrameOfReferenceAtCurrentLocation();

                    HolographicFrame = HolographicSpace.CreateNextFrame();
                    HolographicSurface = HolographicFrame.GetRenderingParameters(HolographicFrame.CurrentPrediction.CameraPoses[0]).Direct3D11BackBuffer;
                    HolographicBackBuffer = UpdateBackBuffer();

                    renderTarget = Texture.New2D(
                        GraphicsDevice,
                        PresentationParameters.BackBufferFormat,
                        PresentationParameters.BackBufferWidth,
                        presentationParameters.BackBufferHeight,
                        DescriptorHeapType.RenderTargetView,
                        resourceFlags: ResourceFlags.AllowRenderTarget,
                        arraySize: BufferCount,
                        mipLevels: 1);

                    using (SharpDX.Direct3D11.Device11On12 device11On12 = GraphicsDevice.NativeDirect3D11Device.QueryInterface<SharpDX.Direct3D11.Device11On12>())
                    {
                        device11On12.CreateWrappedResource(
                            BackBuffer.NativeResource,
                            new SharpDX.Direct3D11.D3D11ResourceFlags { BindFlags = (int)Direct3DBindings.RenderTarget },
                            (int)ResourceStates.RenderTarget,
                            (int)ResourceStates.Present,
                            Utilities.GetGuidFromType(typeof(SharpDX.Direct3D11.Resource)),
                            out d3D11RenderTarget);
                    }

                    Resize(PresentationParameters.BackBufferWidth, presentationParameters.BackBufferHeight);
                    break;
                default:
                    throw new NotSupportedException("This app context type is not supported while creating a swap chain.");
            }
        }

        public override event EventHandler<SizeChangedEventArgs> SizeChanged;

        public override Texture BackBuffer => renderTarget;

        public override object NativePresenter => HolographicSpace;

        internal HolographicDisplay HolographicDisplay { get; }

        internal HolographicSpace HolographicSpace { get; }

        internal SpatialStationaryFrameOfReference SpatialStationaryFrameOfReference { get; }

        internal SharpDX.Direct3D11.Texture2D HolographicBackBuffer { get; set; }

        internal HolographicFrame HolographicFrame { get; private set; }

        internal IDirect3DSurface HolographicSurface { get; set; }

        public override void BeginDraw(CommandList commandList)
        {
            HolographicFrame = HolographicSpace.CreateNextFrame();
            HolographicBackBuffer = UpdateBackBuffer();
        }

        public override void Dispose()
        {
            renderTarget.Dispose();
        }

        public override void Present()
        {
            GraphicsDevice.NativeDirect3D11Device.ImmediateContext.CopyResource(d3D11RenderTarget, HolographicBackBuffer);

            HolographicFrame.PresentUsingCurrentPrediction();
        }

        protected unsafe override void ResizeBackBuffer(int width, int height)
        {
            using (SharpDX.Direct3D11.Device11On12 device11On12 = GraphicsDevice.NativeDirect3D11Device.QueryInterface<SharpDX.Direct3D11.Device11On12>())
            {
                device11On12.ReleaseWrappedResources(new[] { d3D11RenderTarget }, 1);

                renderTarget.Dispose();
                renderTarget = Texture.New2D(
                    GraphicsDevice,
                    PresentationParameters.BackBufferFormat,
                    width,
                    height,
                    DescriptorHeapType.RenderTargetView,
                    resourceFlags: ResourceFlags.AllowRenderTarget,
                    arraySize: BufferCount,
                    mipLevels: 1);

                device11On12.CreateWrappedResource(
                    BackBuffer.NativeResource,
                    new SharpDX.Direct3D11.D3D11ResourceFlags() { BindFlags = (int)Direct3DBindings.RenderTarget },
                    (int)ResourceStates.RenderTarget,
                    (int)ResourceStates.Present,
                    Utilities.GetGuidFromType(typeof(SharpDX.Direct3D11.Resource)),
                    out d3D11RenderTarget);
            }
        }

        protected override void ResizeDepthStencilBuffer(int width, int height)
        {
            DepthStencilBuffer.Dispose();
            DepthStencilBuffer = CreateDepthStencilBuffer(width, height);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //PresentationParameters.BackBufferWidth = e.Width;
            //PresentationParameters.BackBufferHeight = e.Height;
        }

        private SharpDX.Direct3D11.Texture2D UpdateBackBuffer()
        {
            HolographicSurface = HolographicFrame.GetRenderingParameters(HolographicFrame.CurrentPrediction.CameraPoses[0]).Direct3D11BackBuffer;
            SharpDX.Direct3D11.Texture2D d3DBackBuffer = new SharpDX.Direct3D11.Texture2D(GraphicsDevice.CreateDXGISurface(HolographicSurface).NativePointer);

            PresentationParameters.BackBufferFormat = d3DBackBuffer.Description.Format;
            PresentationParameters.BackBufferWidth = d3DBackBuffer.Description.Width;
            PresentationParameters.BackBufferHeight = d3DBackBuffer.Description.Height;

            return d3DBackBuffer;
        }

        //private void WriteToHolographicBackBuffer()
        //{
        //for (int i = 0; i < BufferCount; i++)
        //{
        //copyCommandList.Reset();
        //copyCommandList.CopyTextureRegion(destLocations[i], srcLocations[i]);
        //copyCommandList.Flush(true);

        //Direct3D11Device.ImmediateContext.UpdateSubresource(
        //    new DataBox(
        //        copyTextures[i].MappedResource,
        //        copyableFootprints[i].Footprint.RowPitch,
        //        copyTextures[i].Width),
        //        HolographicBackBuffer,
        //        i);
        //}
        //}

        //for (int i = 0; i < BufferCount; i++)
        //{
        //    copyTextures[i]?.Dispose();

        //    PlacedSubResourceFootprint[] footprints = { new PlacedSubResourceFootprint() };
        //    var description = renderTarget.NativeResource.Description;
        //    GraphicsDevice.NativeDevice.GetCopyableFootprints(ref description, i, 1, 0, footprints, null, null, out long totalBytes);

        //    copyTextures[i] = Texture.NewBuffer(GraphicsDevice, (int)totalBytes, resourceStates: ResourceStates.CopyDestination, heapType: HeapType.Readback);

        //    srcLocations[i] = new TextureCopyLocation(renderTarget.NativeResource, i);
        //    destLocations[i] = new TextureCopyLocation(copyTextures[i].NativeResource, new PlacedSubResourceFootprint
        //    {
        //        Footprint = footprints[0].Footprint
        //    });

        //    copyTextures[i].Map(0);
        //    copyableFootprints[i] = footprints[0];
        //}

        //Direct3D11Device = new SharpDX.Direct3D11.Device(
        //    SharpDX.Direct3D.DriverType.Hardware,
        //    SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
        //    GraphicsDevice.FeatureLevel);
    }
}
