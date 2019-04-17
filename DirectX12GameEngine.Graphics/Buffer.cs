using System;
using DirectX12GameEngine.Core;
using SharpDX.Direct3D12;

using Resource = SharpDX.Direct3D12.Resource;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer : GraphicsResource
    {
        protected internal Buffer(GraphicsDevice device, Resource resource, BufferDescription description) : base(device, resource)
        {
            Description = description;

            (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = description.Flags switch
            {
                BufferFlags.ConstantBuffer => CreateConstantBufferView(),
                _ => default
            };
        }

        public BufferDescription Description { get; }

        public int SizeInBytes => Description.SizeInBytes;

        public BufferFlags Flags => Description.Flags;

        public GraphicsResourceUsage Usage => Description.Usage;

        public unsafe void SetData<T>(in T data) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                SetData(new Span<T>(pointer, 1));
            }
        }

        public void SetData<T>(Span<T> data) where T : unmanaged
        {
            if (Usage != GraphicsResourceUsage.Default)
            {
                Map(0);
                MemoryHelper.Copy(data, MappedResource);
                Unmap(0);
            }
        }

        public static Buffer New(GraphicsDevice device, BufferDescription description)
        {
            Resource resource = device.NativeDevice.CreateCommittedResource(
                new HeapProperties((HeapType)description.Usage), HeapFlags.None,
                ConvertToNativeDescription(description), ResourceStates.GenericRead);

            return new Buffer(device, resource, description);
        }

        public static Buffer New(GraphicsDevice device, int size, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, new BufferDescription(size, bufferFlags, usage));
        }

        public static unsafe Buffer New<T>(GraphicsDevice device, in T data, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                return New(device, new Span<T>(pointer, 1), bufferFlags, usage);
            }
        }

        public static unsafe Buffer New<T>(GraphicsDevice device, Span<T> data, BufferFlags bufferFlags, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
        {
            int bufferSize = data.Length * sizeof(T);
            Buffer buffer = New(device, bufferSize, bufferFlags, usage);

            if (usage == GraphicsResourceUsage.Upload)
            {
                buffer.SetData(data);
            }
            else
            {
                using Buffer uploadBuffer = New(device, data, BufferFlags.None, GraphicsResourceUsage.Upload);

                CommandList copyCommandList = device.GetOrCreateCopyCommandList();

                copyCommandList.CopyResource(uploadBuffer, buffer);
                copyCommandList.Flush(true);

                device.CopyCommandLists.Enqueue(copyCommandList);
            }

            return buffer;
        }

        private static ResourceDescription ConvertToNativeDescription(BufferDescription description)
        {
            int size = description.SizeInBytes;
            ResourceFlags flags = ResourceFlags.None;

            if (description.Flags.HasFlag(BufferFlags.UnorderedAcces))
            {
                flags |= ResourceFlags.AllowUnorderedAccess;
            }

            return ResourceDescription.Buffer(size, flags);
        }

        private (CpuDescriptorHandle, GpuDescriptorHandle) CreateConstantBufferView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

            int constantBufferSize = (SizeInBytes + 255) & ~255;

            ConstantBufferViewDescription cbvDescription = new ConstantBufferViewDescription
            {
                BufferLocation = NativeResource.GPUVirtualAddress,
                SizeInBytes = constantBufferSize
            };

            GraphicsDevice.NativeDevice.CreateConstantBufferView(cbvDescription, cpuHandle);

            return (cpuHandle, gpuHandle);
        }
    }
}
