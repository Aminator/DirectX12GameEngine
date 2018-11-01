using System;
using SharpDX;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine
{
    public sealed class Texture : IDisposable
    {
        public Texture(GraphicsDevice device, Resource resource, DescriptorHeapType? descriptorHeapType = null)
        {
            GraphicsDevice = device;
            NativeResource = resource;

            Width = (int)NativeResource.Description.Width;
            Height = NativeResource.Description.Height;

            switch (descriptorHeapType)
            {
                case DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView:
                    (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = CreateShaderResourceView();
                    break;
                case DescriptorHeapType.RenderTargetView:
                    (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = CreateRenderTargetView();
                    break;
                case DescriptorHeapType.DepthStencilView:
                    (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = CreateDepthStencilView();
                    break;
            }
        }

        public GraphicsDevice GraphicsDevice { get; }

        public int Height { get; }

        public int Width { get; }

        public IntPtr MappedResource { get; private set; }

        internal CpuDescriptorHandle NativeCpuDescriptorHandle { get; }

        internal GpuDescriptorHandle NativeGpuDescriptorHandle { get; }

        internal Resource NativeResource { get; }

        public static Texture New(GraphicsDevice device, HeapProperties heapProperties, ResourceDescription resourceDescription, DescriptorHeapType? descriptorHeapType = null, ResourceStates resourceStates = ResourceStates.GenericRead, ResourceFlags resourceFlags = ResourceFlags.None)
        {
            return new Texture(device, device.NativeDevice.CreateCommittedResource(
                heapProperties, HeapFlags.None,
                resourceDescription, resourceStates), descriptorHeapType);
        }

        public static Texture New2D(GraphicsDevice device, SharpDX.DXGI.Format format, int width, int height, DescriptorHeapType? descriptorHeapType = null, ResourceStates resourceStates = ResourceStates.GenericRead, ResourceFlags resourceFlags = ResourceFlags.None, HeapType heapType = HeapType.Default, short arraySize = 1, short mipLevels = 0)
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

        public static Texture CreateConstantBufferView<T>(GraphicsDevice device, in T data) where T : unmanaged
        {
            Span<T> span = stackalloc T[] { data };
            return CreateConstantBufferView(device, span);
        }

        public static unsafe Texture CreateConstantBufferView<T>(GraphicsDevice device, Span<T> data) where T : unmanaged
        {
            int bufferSize = data.Length * sizeof(T);

            Texture constantBuffer = CreateConstantBufferView(device, bufferSize);

            Utilities.Write(constantBuffer.MappedResource, data.ToArray(), 0, data.Length);

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
