using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
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

        public static async Task<Texture> LoadAsync(GraphicsDevice device, string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            return await LoadAsync(device, stream);
        }

        public static async Task<Texture> LoadAsync(GraphicsDevice device, Stream stream)
        {
            using Image image = await Image.LoadAsync(stream);
            return New2D(device, image.Data.Span, image.Width, image.Height, image.Description.Format);
        }

        public static Texture New(GraphicsDevice device, TextureDescription description)
        {
            return new Texture(device).InitializeFrom(description);
        }

        public static Texture New2D(GraphicsDevice device, int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, short mipCount = 1, short arraySize = 1, int multisampleCount = 1, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return New(device, TextureDescription.New2D(width, height, format, textureFlags, mipCount, arraySize, multisampleCount, heapType));
        }

        public static Texture New2D<T>(GraphicsDevice device, Span<T> data, int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, short mipCount = 1, short arraySize = 1, int sampleCount = 1, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            Texture texture = New2D(device, width, height, format, textureFlags, mipCount, arraySize, sampleCount, heapType);
            texture.SetData(data);

            return texture;
        }

        public unsafe void SetData<T>(Span<T> data) where T : unmanaged
        {
            if (NativeResource is null) throw new InvalidOperationException();

            ID3D12Resource uploadResource = GraphicsDevice.NativeDevice.CreateCommittedResource(new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), HeapFlags.None, NativeResource.Description, ResourceStates.CopyDestination);
            using Texture textureUploadBuffer = new Texture(GraphicsDevice).InitializeFrom(uploadResource);

            textureUploadBuffer.NativeResource!.WriteToSubresource(0, data, Width * 4, Width * Height * 4);

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

        internal Texture InitializeFrom(ID3D12Resource resource, bool isShaderResource = false)
        {
            resource.GetHeapProperties(out HeapProperties heapProperties, out _);

            TextureDescription description = ConvertFromNativeDescription(resource.Description, (GraphicsHeapType)heapProperties.Type, isShaderResource);

            return InitializeFrom(resource, description);
        }

        internal Texture InitializeFrom(ID3D12Resource resource, TextureDescription description)
        {
            NativeResource = resource;
            Description = description;

            if (description.Flags.HasFlag(TextureFlags.DepthStencil))
            {
                NativeDepthStencilView = CreateDepthStencilView();
            }

            if (description.Flags.HasFlag(TextureFlags.RenderTarget))
            {
                NativeRenderTargetView = CreateRenderTargetView();
            }

            if (description.Flags.HasFlag(TextureFlags.ShaderResource))
            {
                NativeShaderResourceView = CreateShaderResourceView();
            }

            if (description.Flags.HasFlag(TextureFlags.UnorderedAccess))
            {
                NativeUnorderedAccessView = CreateUnorderedAccessView();
            }

            return this;
        }

        internal static ResourceFlags GetBindFlagsFromTextureFlags(TextureFlags flags)
        {
            ResourceFlags result = ResourceFlags.None;

            if (flags.HasFlag(TextureFlags.RenderTarget))
            {
                result |= ResourceFlags.AllowRenderTarget;
            }

            if (flags.HasFlag(TextureFlags.UnorderedAccess))
            {
                result |= ResourceFlags.AllowUnorderedAccess;
            }

            if (flags.HasFlag(TextureFlags.DepthStencil))
            {
                result |= ResourceFlags.AllowDepthStencil;

                if (!flags.HasFlag(TextureFlags.ShaderResource))
                {
                    result |= ResourceFlags.DenyShaderResource;
                }
            }

            return result;
        }

        private static TextureDescription ConvertFromNativeDescription(ResourceDescription description, GraphicsHeapType heapType, bool isShaderResource = false)
        {
            TextureDescription textureDescription = new TextureDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = (int)description.Width,
                Height = description.Height,
                SampleCount = description.SampleDescription.Count,
                Format = (PixelFormat)description.Format,
                MipLevels = description.MipLevels,
                HeapType = heapType,
                DepthOrArraySize = description.DepthOrArraySize,
                Flags = TextureFlags.None
            };

            if (description.Flags.HasFlag(ResourceFlags.AllowRenderTarget))
            {
                textureDescription.Flags |= TextureFlags.RenderTarget;
            }

            if (description.Flags.HasFlag(ResourceFlags.AllowUnorderedAccess))
            {
                textureDescription.Flags |= TextureFlags.UnorderedAccess;
            }

            if (description.Flags.HasFlag(ResourceFlags.AllowDepthStencil))
            {
                textureDescription.Flags |= TextureFlags.DepthStencil;
            }

            if (!description.Flags.HasFlag(ResourceFlags.DenyShaderResource) && isShaderResource)
            {
                textureDescription.Flags |= TextureFlags.ShaderResource;
            }

            return textureDescription;
        }

        private static ResourceDescription ConvertToNativeDescription(TextureDescription description)
        {
            return description.Dimension switch
            {
                TextureDimension.Texture2D => ResourceDescription.Texture2D((Format)description.Format, description.Width, description.Height, description.DepthOrArraySize, description.MipLevels, description.SampleCount, 0, GetBindFlagsFromTextureFlags(description.Flags)),
                _ => throw new NotSupportedException()
            };
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
            GraphicsDevice.NativeDevice.CreateRenderTargetView(NativeResource, null, cpuHandle);

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
    }
}
