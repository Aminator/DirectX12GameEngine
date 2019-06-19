using System;
using DirectX12GameEngine.Core;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer : GraphicsResource
    {
        public Buffer()
        {
        }

        protected Buffer(GraphicsDevice device) : base(device)
        {
        }

        public BufferDescription Description { get; private set; }

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
            return new Buffer(device).InitializeFrom(description);
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
            buffer.Recreate(data);

            return buffer;
        }

        protected internal Buffer InitializeFrom(BufferDescription description)
        {
            NativeResource ??= GraphicsDevice.NativeDevice.CreateCommittedResource(
                new HeapProperties((HeapType)description.Usage), HeapFlags.None,
                ConvertToNativeDescription(description), ResourceStates.GenericRead);

            Description = description;

            (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = description.Flags switch
            {
                BufferFlags.ConstantBuffer => CreateConstantBufferView(),
                _ => default
            };

            return this;
        }

        protected internal void Recreate<T>(Span<T> data) where T : unmanaged
        {
            if (Description.Usage == GraphicsResourceUsage.Upload)
            {
                SetData(data);
            }
            else
            {
                using Buffer uploadBuffer = New(GraphicsDevice, data, BufferFlags.None, GraphicsResourceUsage.Upload);

                CommandList copyCommandList = GraphicsDevice.GetOrCreateCopyCommandList();

                copyCommandList.CopyResource(uploadBuffer, this);
                copyCommandList.Flush(true);

                GraphicsDevice.EnqueueCopyCommandList(copyCommandList);
            }
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
