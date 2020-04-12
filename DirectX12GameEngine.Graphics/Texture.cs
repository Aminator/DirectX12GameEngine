using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    [TypeConverter(typeof(AssetReferenceTypeConverter))]
    public class Texture : GraphicsResource
    {
        IntPtr nativeRenderTargetView;
        IntPtr nativeShaderResourceView;
        IntPtr nativeUnorderedAccessView;
        IntPtr nativeDepthStencilView;

        public Texture(GraphicsDevice device, TextureDescription description) : base(device, CreateResource(device, description))
        {
            InitializeFromDescription(description);
        }

        protected Texture(GraphicsDevice device, ID3D12Resource resource, bool isSRgb = false) : base(device, resource)
        {
            InitializeFromResource(isSRgb);
        }

        public TextureDescription Description { get; private set; }

        public int Width => Description.Width;

        public int Height => Description.Height;

        public override IntPtr NativeRenderTargetView => nativeRenderTargetView = nativeRenderTargetView != IntPtr.Zero ? nativeRenderTargetView : CreateRenderTargetView();

        public override IntPtr NativeShaderResourceView => nativeShaderResourceView = nativeShaderResourceView != IntPtr.Zero ? nativeShaderResourceView : CreateShaderResourceView();

        public override IntPtr NativeUnorderedAccessView => nativeUnorderedAccessView = nativeUnorderedAccessView != IntPtr.Zero ? nativeUnorderedAccessView : CreateUnorderedAccessView();

        public override IntPtr NativeDepthStencilView => nativeDepthStencilView = nativeDepthStencilView != IntPtr.Zero ? nativeDepthStencilView : CreateDepthStencilView();

        public override IntPtr NativeConstantBufferView => default;

        public static async Task<Texture> LoadAsync(GraphicsDevice device, string filePath, bool isSRgb = false)
        {
            using FileStream stream = File.OpenRead(filePath);
            return await LoadAsync(device, stream, isSRgb);
        }

        public static async Task<Texture> LoadAsync(GraphicsDevice device, Stream stream, bool isSRgb = false)
        {
            using Image image = await Image.LoadAsync(stream, isSRgb);
            return New2D(device, image.Data.Span, image.Width, image.Height, image.Description.Format);
        }

        public static Texture New(GraphicsDevice device, TextureDescription description)
        {
            return new Texture(device, description);
        }

        public static Texture New2D(GraphicsDevice device, int width, int height, PixelFormat format, ResourceFlags textureFlags = ResourceFlags.None, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return New(device, TextureDescription.New2D(width, height, format, textureFlags, mipLevels, arraySize, sampleCount, sampleQuality, heapType));
        }

        public static Texture New2D<T>(GraphicsDevice device, Span<T> data, int width, int height, PixelFormat format, ResourceFlags textureFlags = ResourceFlags.None, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            Texture texture = New2D(device, width, height, format, textureFlags, mipLevels, arraySize, sampleCount, sampleQuality, heapType);
            texture.SetData(data);

            return texture;
        }

        internal static Texture CreateFromResource(GraphicsDevice device, ID3D12Resource resource, bool isSRgb = false)
        {
            return new Texture(device, resource, isSRgb);
        }

        public unsafe void SetData<T>(Span<T> data) where T : unmanaged
        {
            if (NativeResource is null) throw new InvalidOperationException();

            ID3D12Resource uploadResource = GraphicsDevice.NativeDevice.CreateCommittedResource(new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), HeapFlags.None, NativeResource.Description, ResourceStates.CopyDestination);
            using Texture textureUploadBuffer = new Texture(GraphicsDevice, uploadResource);

            textureUploadBuffer.NativeResource.WriteToSubresource(0, data, Width * 4, Width * Height * 4);

            using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

            copyCommandList.CopyResource(textureUploadBuffer, this);
            copyCommandList.Flush();
        }

        protected void InitializeFromResource(bool isSRgb)
        {
            NativeResource.GetHeapProperties(out HeapProperties heapProperties, out _);

            TextureDescription description = ConvertFromNativeDescription(NativeResource.Description, (GraphicsHeapType)heapProperties.Type);

            if (isSRgb)
            {
                description.Format = description.Format.ToSRgb();
            }

            InitializeFromDescription(description);
        }

        protected void InitializeFromDescription(TextureDescription description)
        {
            Description = description;
        }

        private IntPtr CreateDepthStencilView()
        {
            IntPtr cpuHandle = GraphicsDevice.DepthStencilViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateDepthStencilView(NativeResource, null, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private IntPtr CreateRenderTargetView()
        {
            IntPtr cpuHandle = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);

            RenderTargetViewDescription? rtvDescription = null;

            if (Description.Dimension == TextureDimension.Texture2D)
            {
                RenderTargetViewDescription rtvTexture2DDescription = new RenderTargetViewDescription
                {
                    Format = (Format)Description.Format,
                    ViewDimension = Description.DepthOrArraySize > 1 ? RenderTargetViewDimension.Texture2DArray : RenderTargetViewDimension.Texture2D,
                };

                if (Description.DepthOrArraySize > 1)
                {
                    rtvTexture2DDescription.Texture2DArray.ArraySize = Description.DepthOrArraySize;
                }

                rtvDescription = rtvTexture2DDescription;
            }

            GraphicsDevice.NativeDevice.CreateRenderTargetView(NativeResource, rtvDescription, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private IntPtr CreateShaderResourceView()
        {
            IntPtr cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateShaderResourceView(NativeResource, null, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private IntPtr CreateUnorderedAccessView()
        {
            IntPtr cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateUnorderedAccessView(NativeResource, null, null, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private static ID3D12Resource CreateResource(GraphicsDevice device, TextureDescription description)
        {
            ResourceStates resourceStates = ResourceStates.Common;

            if (description.HeapType == GraphicsHeapType.Upload)
            {
                resourceStates = ResourceStates.GenericRead;
            }
            else if (description.HeapType == GraphicsHeapType.Readback)
            {
                resourceStates = ResourceStates.CopyDestination;
            }

            return device.NativeDevice.CreateCommittedResource(
                new HeapProperties((HeapType)description.HeapType), HeapFlags.None,
                ConvertToNativeDescription(description), resourceStates);
        }

        private static TextureDescription ConvertFromNativeDescription(ResourceDescription description, GraphicsHeapType heapType)
        {
            return new TextureDescription
            {
                Dimension = (TextureDimension)description.Dimension,
                Width = (int)description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = (PixelFormat)description.Format,
                Flags = (ResourceFlags)description.Flags,
                SampleDescription = new SampleDescription(description.SampleDescription.Count, description.SampleDescription.Quality),
                HeapType = heapType,
            };
        }

        private static ResourceDescription ConvertToNativeDescription(TextureDescription description)
        {
            return new ResourceDescription
            {
                Dimension = (ResourceDimension)description.Dimension,
                Width = description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = (Format)description.Format,
                Flags = (Vortice.Direct3D12.ResourceFlags)description.Flags,
                SampleDescription = new Vortice.DXGI.SampleDescription(description.SampleDescription.Count, description.SampleDescription.Quality)
            };
        }
    }
}
