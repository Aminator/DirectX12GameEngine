using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DirectX12GameEngine.Core;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public partial class Buffer : GraphicsResource
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Buffer()
        {
        }

        protected internal Buffer(GraphicsDevice device) : base(device)
        {
        }

        public BufferDescription Description { get; private set; }

        public int SizeInBytes => Description.SizeInBytes;

        public BufferFlags Flags => Description.Flags;

        public GraphicsHeapType Usage => Description.HeapType;

        public int ElementCount { get; protected set; }

        public static Buffer New(GraphicsDevice device, BufferDescription description)
        {
            return new Buffer(device).InitializeFrom(description);
        }

        public static Buffer New(GraphicsDevice device, int size, BufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return New(device, new BufferDescription(size, bufferFlags, heapType));
        }

        public static Buffer<T> New<T>(GraphicsDevice device, int elementCount, BufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            int size = elementCount * Unsafe.SizeOf<T>();

            return new Buffer<T>(device, new BufferDescription(size, bufferFlags, heapType));
        }

        public static unsafe Buffer<T> New<T>(GraphicsDevice device, in T data, BufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                return New(device, new Span<T>(pointer, 1), bufferFlags, heapType);
            }
        }

        public static Buffer<T> New<T>(GraphicsDevice device, Span<T> data, BufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            Buffer<T> buffer = New<T>(device, data.Length, bufferFlags, heapType);
            buffer.SetData(data);

            return buffer;
        }

        public T[] GetData<T>() where T : unmanaged
        {
            T[] data = new T[SizeInBytes / Unsafe.SizeOf<T>()];
            GetData(data.AsSpan());

            return data;
        }

        public unsafe void GetData<T>(ref T data, int offsetInBytes = 0) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                GetData(new Span<T>(pointer, 1), offsetInBytes);
            }
        }

        public void GetData<T>(Span<T> data, int offsetInBytes = 0) where T : unmanaged
        {
            if (Usage == GraphicsHeapType.Default)
            {
                using Buffer<T> readbackBaffer = New<T>(GraphicsDevice, data.Length, BufferFlags.None, GraphicsHeapType.Readback);
                using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                copyCommandList.CopyBufferRegion(this, offsetInBytes, readbackBaffer, 0, data.Length * Unsafe.SizeOf<T>());
                copyCommandList.Flush(true);

                readbackBaffer.GetData(data);
            }
            else
            {
                Map(0);
                MemoryHelper.Copy(MappedResource + offsetInBytes, data);
                Unmap(0);
            }
        }

        public unsafe void SetData<T>(in T data, int offsetInBytes = 0) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                SetData(new Span<T>(pointer, 1), offsetInBytes);
            }
        }

        public void SetData<T>(Span<T> data, int offsetInBytes = 0) where T : unmanaged
        {
            if (Usage == GraphicsHeapType.Default)
            {
                using Buffer<T> uploadBuffer = New(GraphicsDevice, data, BufferFlags.None, GraphicsHeapType.Upload);
                using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                copyCommandList.CopyBufferRegion(uploadBuffer, 0, this, offsetInBytes, data.Length * Unsafe.SizeOf<T>());
                copyCommandList.Flush(true);
            }
            else
            {
                Map(0);
                MemoryHelper.Copy(data, MappedResource + offsetInBytes);
                Unmap(0);
            }
        }

        public Buffer InitializeFrom(BufferDescription description)
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

            Resource resource = GraphicsDevice.NativeDevice.CreateCommittedResource(
                new HeapProperties((HeapType)description.HeapType), HeapFlags.None,
                ConvertToNativeDescription(description), resourceStates);

            return InitializeFrom(resource, description);
        }

        internal Buffer InitializeFrom(Resource resource)
        {
            resource.GetHeapProperties(out HeapProperties heapProperties, out _);

            BufferDescription description = ConvertFromNativeDescription(resource.Description, (GraphicsHeapType)heapProperties.Type);

            return InitializeFrom(resource, description);
        }

        private Buffer InitializeFrom(Resource resource, BufferDescription description)
        {
            NativeResource = resource;

            Description = description;

            (NativeCpuDescriptorHandle, NativeGpuDescriptorHandle) = description.Flags switch
            {
                BufferFlags.ConstantBuffer => CreateConstantBufferView(),
                BufferFlags.ShaderResource => CreateShaderResourceView(),
                BufferFlags.UnorderedAccess => CreateUnorderedAccessView(),
                _ => default
            };

            return this;
        }

        private static BufferDescription ConvertFromNativeDescription(ResourceDescription description, GraphicsHeapType heapType, bool isShaderResource = false)
        {
            BufferDescription bufferDescription = new BufferDescription
            {
                SizeInBytes = (int)description.Width * description.Height * description.DepthOrArraySize,
                HeapType = heapType,
                Flags = BufferFlags.None
            };

            if (description.Flags.HasFlag(ResourceFlags.AllowUnorderedAccess))
            {
                bufferDescription.Flags |= BufferFlags.UnorderedAccess;
            }

            if (!description.Flags.HasFlag(ResourceFlags.DenyShaderResource) && isShaderResource)
            {
                bufferDescription.Flags |= BufferFlags.ShaderResource;
            }

            return bufferDescription;
        }

        private static ResourceDescription ConvertToNativeDescription(BufferDescription description)
        {
            int size = description.SizeInBytes;
            ResourceFlags flags = ResourceFlags.None;

            if (description.Flags.HasFlag(BufferFlags.UnorderedAccess))
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
                BufferLocation = NativeResource!.GPUVirtualAddress,
                SizeInBytes = constantBufferSize
            };

            GraphicsDevice.NativeDevice.CreateConstantBufferView(cbvDescription, cpuHandle);

            return (cpuHandle, gpuHandle);
        }

        private (CpuDescriptorHandle, GpuDescriptorHandle) CreateShaderResourceView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateShaderResourceView(NativeResource, null, cpuHandle);

            return (cpuHandle, gpuHandle);
        }

        private (CpuDescriptorHandle, GpuDescriptorHandle) CreateUnorderedAccessView()
        {
            (CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle) = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

            var description = new UnorderedAccessViewDescription
            {
                Format = SharpDX.DXGI.Format.R32_Float,
                Dimension = UnorderedAccessViewDimension.Buffer,
                Buffer =
                {
                    ElementCount = ElementCount
                }
            };

            GraphicsDevice.NativeDevice.CreateUnorderedAccessView(NativeResource, null, description, cpuHandle);

            return (cpuHandle, gpuHandle);
        }
    }

    public class Buffer<T> : Buffer where T : unmanaged
    {
        protected internal Buffer(GraphicsDevice device, BufferDescription description) : base(device)
        {
            ElementSize = Unsafe.SizeOf<T>();
            ElementCount = description.SizeInBytes / ElementSize;

            InitializeFrom(description);
        }

        public int ElementSize { get; }

        public T[] GetData() => GetData<T>();
    }
}
