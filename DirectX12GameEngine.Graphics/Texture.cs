using System;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Core;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using SharpDX.WIC;

using Resource = SharpDX.Direct3D12.Resource;

namespace DirectX12GameEngine.Graphics
{
    public sealed class Texture : IDisposable
    {
        public Texture(GraphicsDevice device, Resource resource, DescriptorHeapType? descriptorHeapType = null)
        {
            GraphicsDevice = device;
            NativeResource = resource;

            Width = (int)NativeResource.Description.Width;
            Height = NativeResource.Description.Height;

            (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = descriptorHeapType switch
            {
                DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView => CreateShaderResourceView(),
                DescriptorHeapType.RenderTargetView => CreateRenderTargetView(),
                DescriptorHeapType.DepthStencilView => CreateDepthStencilView(),
                _ => default
            };
        }

        public GraphicsDevice GraphicsDevice { get; }

        public int Width { get; }

        public int Height { get; }

        public IntPtr MappedResource { get; private set; }

        internal CpuDescriptorHandle NativeCpuDescriptorHandle { get; }

        internal GpuDescriptorHandle NativeGpuDescriptorHandle { get; }

        internal Resource NativeResource { get; }

        public static Texture New(GraphicsDevice device, HeapProperties heapProperties, ResourceDescription resourceDescription, DescriptorHeapType? descriptorHeapType = null, ResourceStates resourceStates = ResourceStates.GenericRead)
        {
            return new Texture(device, device.NativeDevice.CreateCommittedResource(
                heapProperties, HeapFlags.None,
                resourceDescription, resourceStates), descriptorHeapType);
        }

        public static Texture New2D(GraphicsDevice device, Format format, int width, int height, DescriptorHeapType? descriptorHeapType = null, ResourceStates resourceStates = ResourceStates.GenericRead, ResourceFlags resourceFlags = ResourceFlags.None, HeapType heapType = HeapType.Default, short arraySize = 1, short mipLevels = 0)
        {
            return new Texture(device, device.NativeDevice.CreateCommittedResource(
                new HeapProperties(heapType), HeapFlags.None,
                ResourceDescription.Texture2D(format, width, height, arraySize, mipLevels, flags: resourceFlags), resourceStates), descriptorHeapType);
        }

        public static Texture NewBuffer(GraphicsDevice device, int size, DescriptorHeapType? descriptorHeapType = null, ResourceStates resourceStates = ResourceStates.GenericRead, ResourceFlags resourceFlags = ResourceFlags.None, HeapType heapType = HeapType.Upload)
        {
            return new Texture(device, device.NativeDevice.CreateCommittedResource(
                new HeapProperties(heapType), HeapFlags.None,
                ResourceDescription.Buffer(size, resourceFlags), resourceStates), descriptorHeapType);
        }

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
                bitmapSource.Initialize(decoder.GetFrame(0), PixelFormat.Format32bppBGRA);

                Format pixelFormat = Format.B8G8R8A8_UNorm;
                int stride = bitmapSource.Size.Width * FormatHelper.SizeOfInBytes(pixelFormat);
                byte[] imageBuffer = new byte[stride * bitmapSource.Size.Height];

                bitmapSource.CopyPixels(imageBuffer, stride);

                return CreateTexture2D(device, imageBuffer.AsSpan(), pixelFormat, bitmapSource.Size.Width, bitmapSource.Size.Height);
            });
        }

        public static unsafe Texture CreateBuffer<T>(GraphicsDevice device, Span<T> data, DescriptorHeapType? descriptorHeapType = null) where T : unmanaged
        {
            int bufferSize = data.Length * sizeof(T);

            Texture buffer = NewBuffer(device, bufferSize, descriptorHeapType, ResourceStates.CopyDestination, heapType: HeapType.Default);
            using Texture uploadBuffer = New(device, new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), buffer.NativeResource.Description);

            IntPtr uploadPointer = uploadBuffer.Map(0);
            MemoryHelper.Copy(data, uploadPointer);
            uploadBuffer.Unmap(0);

            CommandList copyCommandList = device.GetOrCreateCopyCommandList();

            copyCommandList.CopyResource(uploadBuffer, buffer);
            copyCommandList.Flush(true);

            device.CopyCommandLists.Enqueue(copyCommandList);

            return buffer;
        }

        public static unsafe Texture CreateConstantBufferView<T>(GraphicsDevice device, in T data) where T : unmanaged
        {
            int bufferSize = sizeof(T);

            Texture constantBuffer = CreateConstantBufferView(device, bufferSize);
            MemoryHelper.Copy(data, constantBuffer.MappedResource);

            return constantBuffer;
        }

        public static unsafe Texture CreateConstantBufferView<T>(GraphicsDevice device, Span<T> data) where T : unmanaged
        {
            int bufferSize = data.Length * sizeof(T);

            Texture constantBuffer = CreateConstantBufferView(device, bufferSize);
            MemoryHelper.Copy(data, constantBuffer.MappedResource);

            return constantBuffer;
        }

        public static unsafe Texture CreateConstantBufferView(GraphicsDevice device, int bufferSize)
        {
            int constantBufferSize = (bufferSize + 255) & ~255;

            Texture constantBuffer = NewBuffer(device, constantBufferSize, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

            ConstantBufferViewDescription cbvDescription = new ConstantBufferViewDescription
            {
                BufferLocation = constantBuffer.NativeResource.GPUVirtualAddress,
                SizeInBytes = constantBufferSize
            };

            device.NativeDevice.CreateConstantBufferView(cbvDescription, constantBuffer.NativeCpuDescriptorHandle);
            constantBuffer.Map(0);

            return constantBuffer;
        }

        public static IndexBufferView CreateIndexBufferView(Texture indexBuffer, Format format, int size)
        {
            switch (format)
            {
                case Format.R16_UInt:
                case Format.R32_UInt:
                    break;
                default:
                    throw new NotSupportedException("Index buffer type must be ushort or uint");
            }

            return new IndexBufferView
            {
                BufferLocation = indexBuffer.NativeResource.GPUVirtualAddress,
                Format = format,
                SizeInBytes = size
            };
        }

        public static IndexBufferView CreateIndexBufferView<T>(GraphicsDevice device, Span<T> indices, Format format, out Texture indexBuffer) where T : unmanaged
        {
            indexBuffer = CreateBuffer(device, indices);

            int indexBufferSize = indexBuffer.Width * indexBuffer.Height;

            return CreateIndexBufferView(indexBuffer, format, indexBufferSize);
        }

        public static VertexBufferView CreateVertexBufferView(Texture vertexBuffer, int size, int stride)
        {
            return new VertexBufferView
            {
                BufferLocation = vertexBuffer.NativeResource.GPUVirtualAddress,
                StrideInBytes = stride,
                SizeInBytes = size
            };
        }

        public static unsafe VertexBufferView CreateVertexBufferView<T>(Texture vertexBuffer, int size) where T : unmanaged
        {
            return CreateVertexBufferView(vertexBuffer, size, sizeof(T));
        }

        public static VertexBufferView CreateVertexBufferView<T>(GraphicsDevice device, Span<T> vertices, out Texture vertexBuffer, int? stride = null) where T : unmanaged
        {
            vertexBuffer = CreateBuffer(device, vertices);

            int vertexBufferSize = vertexBuffer.Width * vertexBuffer.Height;

            return stride.HasValue
                ? CreateVertexBufferView(vertexBuffer, vertexBufferSize, stride.Value)
                : CreateVertexBufferView<T>(vertexBuffer, vertexBufferSize);
        }

        public static unsafe Texture CreateTexture2D<T>(GraphicsDevice device, Span<T> data, Format format, int width, int height) where T : unmanaged
        {
            int texturePixelSize = format switch
            {
                Format.R8G8B8A8_UNorm => 4,
                Format.B8G8R8A8_UNorm => 4,
                Format.R8G8B8A8_UNorm_SRgb => 4,
                Format.B8G8R8A8_UNorm_SRgb => 4,
                _ => throw new NotSupportedException("This format is not supported.")
            };

            Texture texture = New2D(device, format, width, height, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, ResourceStates.CopyDestination);
            using Texture textureUploadBuffer = New(device, new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), texture.NativeResource.Description);

            fixed (T* ptr = data)
            {
                textureUploadBuffer.NativeResource.WriteToSubresource(0, null, (IntPtr)ptr, texturePixelSize * width, data.Length * sizeof(T));
            }

            ShaderResourceViewDescription srvDescription = new ShaderResourceViewDescription
            {
                Shader4ComponentMapping = D3DXUtilities.DefaultComponentMapping(),
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = { MipLevels = 1 },
            };

            device.NativeDevice.CreateShaderResourceView(texture.NativeResource, srvDescription, texture.NativeCpuDescriptorHandle);

            CommandList copyCommandList = device.GetOrCreateCopyCommandList();

            copyCommandList.CopyResource(textureUploadBuffer, texture);
            copyCommandList.Flush(true);

            device.CopyCommandLists.Enqueue(copyCommandList);

            return texture;
        }

        public (CpuDescriptorHandle, GpuDescriptorHandle) CreateDepthStencilView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.DepthStencilViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateDepthStencilView(NativeResource, null, cpuHandle);

            return (cpuHandle, gpuHandle);
        }

        public (CpuDescriptorHandle, GpuDescriptorHandle) CreateRenderTargetView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateRenderTargetView(NativeResource, null, cpuHandle);

            return (cpuHandle, gpuHandle);
        }

        public (CpuDescriptorHandle, GpuDescriptorHandle) CreateShaderResourceView()
        {
            return GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
        }

        public void Dispose()
        {
            NativeResource.Dispose();
        }

        public IntPtr Map(int subresource)
        {
            IntPtr mappedResource = NativeResource.Map(subresource);
            MappedResource = mappedResource;
            return mappedResource;
        }

        public void Unmap(int subresource)
        {
            NativeResource.Unmap(subresource);
            MappedResource = IntPtr.Zero;
        }
    }
}
