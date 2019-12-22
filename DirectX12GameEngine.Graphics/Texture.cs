using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    [TypeConverter(typeof(AssetReferenceTypeConverter))]
    public sealed class Texture : GraphicsResource
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Texture()
        {
        }

        internal Texture(GraphicsDevice device) : base(device)
        {
        }

        public TextureDescription Description { get; private set; }

        public int Width => Description.Width;

        public int Height => Description.Height;

        internal CpuDescriptorHandle NativeDepthStencilView { get; private set; }

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
            return new Texture(device).InitializeFrom(description);
        }

        public static Texture New2D(GraphicsDevice device, int width, int height, PixelFormat format, ResourceFlags textureFlags = ResourceFlags.ShaderResource, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return New(device, TextureDescription.New2D(width, height, format, textureFlags, mipLevels, arraySize, sampleCount, sampleQuality, heapType));
        }

        public static Texture New2D<T>(GraphicsDevice device, Span<T> data, int width, int height, PixelFormat format, ResourceFlags textureFlags = ResourceFlags.ShaderResource, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            Texture texture = New2D(device, width, height, format, textureFlags, mipLevels, arraySize, sampleCount, sampleQuality, heapType);
            texture.SetData(data);

            return texture;
        }

        public unsafe void SetData<T>(Span<T> data) where T : unmanaged
        {
            if (NativeResource is null) throw new InvalidOperationException();

            ID3D12Resource uploadResource = GraphicsDevice.NativeDevice.CreateCommittedResource(new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), HeapFlags.None, NativeResource.Description, ResourceStates.CopyDestination);
            using Texture textureUploadBuffer = new Texture(GraphicsDevice).InitializeFrom(uploadResource);

            textureUploadBuffer.NativeResource.WriteToSubresource(0, data, Width * 4, Width * Height * 4);

            using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

            copyCommandList.CopyResource(textureUploadBuffer, this);
            copyCommandList.Flush(true);
        }

        public Texture InitializeFrom(TextureDescription description)
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

            ID3D12Resource resource = GraphicsDevice.NativeDevice.CreateCommittedResource(
                new HeapProperties((HeapType)description.HeapType), HeapFlags.None,
                ConvertToNativeDescription(description), resourceStates);

            return InitializeFrom(resource, description);
        }

        internal Texture InitializeFrom(ID3D12Resource resource, bool isSRgb = false)
        {
            resource.GetHeapProperties(out HeapProperties heapProperties, out _);

            TextureDescription description = ConvertFromNativeDescription(resource.Description, (GraphicsHeapType)heapProperties.Type);

            if (isSRgb)
            {
                description.Format = description.Format.ToSRgb();
            }

            return InitializeFrom(resource, description);
        }

        internal Texture InitializeFrom(ID3D12Resource resource, TextureDescription description)
        {
            NativeResource = resource;
            Description = description;

            if (description.Flags.HasFlag(ResourceFlags.DepthStencil))
            {
                NativeDepthStencilView = CreateDepthStencilView();
            }

            if (description.Flags.HasFlag(ResourceFlags.RenderTarget))
            {
                NativeRenderTargetView = CreateRenderTargetView();
            }

            if (description.Flags.HasFlag(ResourceFlags.ShaderResource))
            {
                NativeShaderResourceView = CreateShaderResourceView();
            }

            if (description.Flags.HasFlag(ResourceFlags.UnorderedAccess))
            {
                NativeUnorderedAccessView = CreateUnorderedAccessView();
            }

            return this;
        }

        internal CpuDescriptorHandle CreateDepthStencilView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.DepthStencilViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateDepthStencilView(NativeResource, null, cpuHandle);

            return cpuHandle;
        }

        internal CpuDescriptorHandle CreateRenderTargetView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);

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

            GraphicsDevice.NativeDevice.CreateRenderTargetView(NativeResource, rtvDescription, cpuHandle);

            return cpuHandle;
        }

        internal CpuDescriptorHandle CreateShaderResourceView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateShaderResourceView(NativeResource, null, cpuHandle);

            return cpuHandle;
        }

        internal CpuDescriptorHandle CreateUnorderedAccessView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateUnorderedAccessView(NativeResource, null, null, cpuHandle);

            return cpuHandle;
        }

        private static TextureDescription ConvertFromNativeDescription(ResourceDescription description, GraphicsHeapType heapType, bool isShaderResource = false)
        {
            ResourceFlags flags = ResourceFlags.None;

            if (description.Flags.HasFlag(Vortice.Direct3D12.ResourceFlags.AllowRenderTarget))
            {
                flags |= ResourceFlags.RenderTarget;
            }

            if (description.Flags.HasFlag(Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess))
            {
                flags |= ResourceFlags.UnorderedAccess;
            }

            if (description.Flags.HasFlag(Vortice.Direct3D12.ResourceFlags.AllowDepthStencil))
            {
                flags |= ResourceFlags.DepthStencil;
            }

            if (!description.Flags.HasFlag(Vortice.Direct3D12.ResourceFlags.DenyShaderResource) && isShaderResource)
            {
                flags |= ResourceFlags.ShaderResource;
            }

            return new TextureDescription
            {
                Dimension = (TextureDimension)description.Dimension,
                Width = (int)description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = (PixelFormat)description.Format,
                Flags = flags,
                SampleDescription = new SampleDescription(description.SampleDescription.Count, description.SampleDescription.Quality),
                HeapType = heapType,
            };
        }

        private static ResourceDescription ConvertToNativeDescription(TextureDescription description)
        {
            Vortice.Direct3D12.ResourceFlags flags = Vortice.Direct3D12.ResourceFlags.None;

            if (description.Flags.HasFlag(ResourceFlags.RenderTarget))
            {
                flags |= Vortice.Direct3D12.ResourceFlags.AllowRenderTarget;
            }

            if (description.Flags.HasFlag(ResourceFlags.UnorderedAccess))
            {
                flags |= Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess;
            }

            if (description.Flags.HasFlag(ResourceFlags.DepthStencil))
            {
                flags |= Vortice.Direct3D12.ResourceFlags.AllowDepthStencil;

                if (!description.Flags.HasFlag(ResourceFlags.ShaderResource))
                {
                    flags |= Vortice.Direct3D12.ResourceFlags.DenyShaderResource;
                }
            }

            return new ResourceDescription
            {
                Dimension = (ResourceDimension)description.Dimension,
                Width = description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = (Format)description.Format,
                Flags = flags,
                SampleDescription = new Vortice.DXGI.SampleDescription(description.SampleDescription.Count, description.SampleDescription.Quality)
            };
        }
    }
}
