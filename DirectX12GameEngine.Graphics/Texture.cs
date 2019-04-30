using System;
using System.IO;
using System.Threading.Tasks;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using SharpDX.WIC;

using Resource = SharpDX.Direct3D12.Resource;

namespace DirectX12GameEngine.Graphics
{
    public class Texture : GraphicsResource
    {
        protected internal Texture(GraphicsDevice device, Resource resource) : this(device, resource, ConvertFromNativeDescription(resource.Description))
        {
        }

        protected internal Texture(GraphicsDevice device, Resource resource, TextureDescription description) : base(device, resource)
        {
            Description = description;

            (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = description.Flags switch
            {
                TextureFlags.ShaderResource => CreateShaderResourceView(),
                TextureFlags.RenderTarget => CreateRenderTargetView(),
                TextureFlags.DepthStencil => CreateDepthStencilView(),
                _ => default
            };
        }

        public TextureDescription Description { get; }

        public int Width => Description.Width;

        public int Height => Description.Height;

        public static async Task<Texture> LoadAsync(GraphicsDevice device, string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            return await LoadAsync(device, stream);
        }

        public static unsafe Task<Texture> LoadAsync(GraphicsDevice device, Stream stream)
        {
            return Task.Run(() =>
            {
                ImagingFactory2 imagingFactory = new ImagingFactory2();
                BitmapDecoder decoder = new BitmapDecoder(imagingFactory, stream, DecodeOptions.CacheOnDemand);

                FormatConverter bitmapSource = new FormatConverter(imagingFactory);
                bitmapSource.Initialize(decoder.GetFrame(0), SharpDX.WIC.PixelFormat.Format32bppBGRA);

                PixelFormat pixelFormat = PixelFormat.B8G8R8A8_UNorm;
                int stride = bitmapSource.Size.Width * FormatHelper.SizeOfInBytes((Format)pixelFormat);
                byte[] imageBuffer = new byte[stride * bitmapSource.Size.Height];

                bitmapSource.CopyPixels(imageBuffer, stride);

                return New2D(device, imageBuffer.AsSpan(), bitmapSource.Size.Width, bitmapSource.Size.Height, pixelFormat);
            });
        }

        public static Texture New(GraphicsDevice device, TextureDescription description)
        {
            Resource resource = device.NativeDevice.CreateCommittedResource(
                new HeapProperties((HeapType)description.Usage), HeapFlags.None,
                ConvertToNativeDescription(description), ResourceStates.GenericRead);

            return new Texture(device, resource, description);
        }

        public static Texture New2D(GraphicsDevice device, int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int mipCount = 1, int arraySize = 1, int multisampleCount = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, TextureDescription.New2D(width, height, format, textureFlags, mipCount, arraySize, multisampleCount, usage));
        }

        public static unsafe Texture New2D<T>(GraphicsDevice device, Span<T> data, int width, int height, PixelFormat format) where T : unmanaged
        {
            int texturePixelSize = format switch
            {
                PixelFormat.R8G8B8A8_UNorm => 4,
                PixelFormat.B8G8R8A8_UNorm => 4,
                PixelFormat.R8G8B8A8_UNorm_SRgb => 4,
                PixelFormat.B8G8R8A8_UNorm_SRgb => 4,
                _ => throw new NotSupportedException("This format is not supported.")
            };

            Texture texture = New2D(device, width, height, format);

            Resource uploadResource = device.NativeDevice.CreateCommittedResource(new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), HeapFlags.None, texture.NativeResource.Description, ResourceStates.CopyDestination);
            using Texture textureUploadBuffer = new Texture(device, uploadResource);

            fixed (T* pointer = data)
            {
                textureUploadBuffer.NativeResource.WriteToSubresource(0, null, (IntPtr)pointer, texturePixelSize * width, data.Length * sizeof(T));
            }

            CommandList copyCommandList = device.GetOrCreateCopyCommandList();

            copyCommandList.CopyResource(textureUploadBuffer, texture);
            copyCommandList.Flush(true);

            device.CopyCommandLists.Enqueue(copyCommandList);

            return texture;
        }

        internal static ResourceFlags GetBindFlagsFromTextureFlags(TextureFlags flags)
        {
            var result = ResourceFlags.None;

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

        private static TextureDescription ConvertFromNativeDescription(ResourceDescription description, bool isShaderResource = false)
        {
            TextureDescription textureDescription = new TextureDescription()
            {
                Dimension = TextureDimension.Texture2D,
                Width = (int)description.Width,
                Height = description.Height,
                Depth = 1,
                MultisampleCount = description.SampleDescription.Count,
                Format = (PixelFormat)description.Format,
                MipLevels = description.MipLevels,
                Usage = GraphicsResourceUsage.Default,
                ArraySize = description.DepthOrArraySize,
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
                TextureDimension.Texture2D => ResourceDescription.Texture2D((Format)description.Format, description.Width, description.Height, (short)description.ArraySize, (short)description.MipLevels, description.MultisampleCount, 0, GetBindFlagsFromTextureFlags(description.Flags)),
                _ => throw new NotSupportedException()
            };
        }

        private (CpuDescriptorHandle, GpuDescriptorHandle) CreateDepthStencilView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.DepthStencilViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateDepthStencilView(NativeResource, null, cpuHandle);

            return (cpuHandle, gpuHandle);
        }

        private (CpuDescriptorHandle, GpuDescriptorHandle) CreateRenderTargetView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateRenderTargetView(NativeResource, null, cpuHandle);

            return (cpuHandle, gpuHandle);
        }

        private (CpuDescriptorHandle, GpuDescriptorHandle) CreateShaderResourceView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateShaderResourceView(NativeResource, null, cpuHandle);

            return (cpuHandle, gpuHandle);
        }
    }
}
