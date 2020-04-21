using System;
using System.Runtime.CompilerServices;
using DirectX12GameEngine.Core;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class GraphicsBuffer : GraphicsResource
    {
        IntPtr nativeRenderTargetView;
        IntPtr nativeShaderResourceView;
        IntPtr nativeUnorderedAccessView;
        IntPtr nativeConstantBufferView;

        public GraphicsBuffer(GraphicsDevice device, GraphicsBufferDescription description) : base(device, CreateResource(device, description))
        {
            InitializeFromDescription(description);
        }

        protected GraphicsBuffer(GraphicsDevice device, ID3D12Resource resource) : base(device, resource)
        {
            InitializeFromResource();
        }

        public GraphicsBufferDescription Description { get; private set; }

        public int SizeInBytes => Description.SizeInBytes;

        public ResourceFlags Flags => Description.Flags;

        public GraphicsHeapType HeapType => Description.HeapType;

        public int StructureByteStride => Description.StructureByteStride;

        public int FirstElement { get; protected set; }

        public int ElementCount { get; protected set; }

        public override IntPtr NativeRenderTargetView => nativeRenderTargetView = nativeRenderTargetView != IntPtr.Zero ? nativeRenderTargetView : CreateRenderTargetView();

        public override IntPtr NativeShaderResourceView => nativeShaderResourceView = nativeShaderResourceView != IntPtr.Zero ? nativeShaderResourceView : CreateShaderResourceView();

        public override IntPtr NativeUnorderedAccessView => nativeUnorderedAccessView = nativeUnorderedAccessView != IntPtr.Zero ? nativeUnorderedAccessView : CreateUnorderedAccessView();

        public override IntPtr NativeConstantBufferView => nativeConstantBufferView = nativeConstantBufferView != IntPtr.Zero ? nativeConstantBufferView : CreateConstantBufferView();

        public override IntPtr NativeDepthStencilView => default;

        public static GraphicsBuffer Create(GraphicsDevice device, GraphicsBufferDescription description)
        {
            return new GraphicsBuffer(device, description);
        }

        public static GraphicsBuffer Create(GraphicsDevice device, int size, ResourceFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return Create(device, new GraphicsBufferDescription(size, bufferFlags, heapType));
        }

        public static GraphicsBuffer Create(GraphicsDevice device, int size, int structureByteStride, ResourceFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return Create(device, new GraphicsBufferDescription(size, bufferFlags, heapType, structureByteStride));
        }

        public static GraphicsBuffer<T> Create<T>(GraphicsDevice device, int elementCount, int structureByteStride, ResourceFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            int size = structureByteStride * elementCount;

            return new GraphicsBuffer<T>(device, new GraphicsBufferDescription(size, bufferFlags, heapType, structureByteStride));
        }

        public static GraphicsBuffer<T> Create<T>(GraphicsDevice device, int elementCount, ResourceFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            return Create<T>(device, elementCount, Unsafe.SizeOf<T>(), bufferFlags, heapType);
        }

        public static unsafe GraphicsBuffer<T> Create<T>(GraphicsDevice device, in T data, ResourceFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                return Create(device, new Span<T>(pointer, 1), bufferFlags, heapType);
            }
        }

        public static GraphicsBuffer<T> Create<T>(GraphicsDevice device, Span<T> data, ResourceFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            return Create(device, data, Unsafe.SizeOf<T>(), bufferFlags, heapType);
        }

        public static GraphicsBuffer<T> Create<T>(GraphicsDevice device, Span<T> data, int structureByteStride, ResourceFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            GraphicsBuffer<T> buffer = Create<T>(device, data.Length, structureByteStride, bufferFlags, heapType);
            buffer.SetData(data);

            return buffer;
        }

        internal static GraphicsBuffer CreateFromResource(GraphicsDevice device, ID3D12Resource resource)
        {
            return new GraphicsBuffer(device, resource);
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
            offsetInBytes = FirstElement * StructureByteStride + offsetInBytes;

            if (HeapType == GraphicsHeapType.Default)
            {
                using GraphicsBuffer<T> readbackBaffer = Create<T>(GraphicsDevice, data.Length, ResourceFlags.None, GraphicsHeapType.Readback);
                using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                copyCommandList.CopyBufferRegion(this, offsetInBytes, readbackBaffer, 0, data.Length * Unsafe.SizeOf<T>());
                copyCommandList.Flush();

                readbackBaffer.GetData(data);
            }
            else
            {
                Map(0);
                IntPtr source = MappedResource + offsetInBytes;
                source.CopyTo(data);
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
            offsetInBytes = FirstElement * StructureByteStride + offsetInBytes;

            if (HeapType == GraphicsHeapType.Default)
            {
                using GraphicsBuffer<T> uploadBuffer = Create(GraphicsDevice, data, ResourceFlags.None, GraphicsHeapType.Upload);
                using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                copyCommandList.CopyBufferRegion(uploadBuffer, 0, this, offsetInBytes, data.Length * Unsafe.SizeOf<T>());
                copyCommandList.Flush();
            }
            else
            {
                Map(0);
                data.CopyTo(MappedResource + offsetInBytes);
                Unmap(0);
            }
        }

        public GraphicsBuffer Slice(int start, int length)
        {
            GraphicsBuffer buffer = new GraphicsBuffer(GraphicsDevice, NativeResource);
            buffer.InitializeFromDescription(Description, FirstElement + start, length);

            return buffer;
        }

        protected void InitializeFromResource()
        {
            NativeResource.GetHeapProperties(out HeapProperties heapProperties, out _);

            GraphicsBufferDescription description = ConvertFromNativeDescription(NativeResource.Description, (GraphicsHeapType)heapProperties.Type);
            InitializeFromDescription(description);
        }

        protected void InitializeFromDescription(GraphicsBufferDescription description, int firstElement = 0, int elementCount = 0)
        {
            Description = description;
            FirstElement = firstElement;

            if (description.StructureByteStride != 0)
            {
                ElementCount = elementCount == 0 ? description.SizeInBytes / description.StructureByteStride : elementCount;
            }
        }

        private IntPtr CreateRenderTargetView()
        {
            IntPtr cpuHandle = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);

            RenderTargetViewDescription description = new RenderTargetViewDescription
            {
                ViewDimension = RenderTargetViewDimension.Buffer,
                Buffer =
                {
                    FirstElement = FirstElement,
                    NumElements = ElementCount
                }
            };

            GraphicsDevice.NativeDevice.CreateRenderTargetView(NativeResource, description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private IntPtr CreateConstantBufferView()
        {
            IntPtr cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

            int constantBufferSize = (SizeInBytes + 255) & ~255;

            ConstantBufferViewDescription cbvDescription = new ConstantBufferViewDescription
            {
                BufferLocation = NativeResource.GPUVirtualAddress,
                SizeInBytes = constantBufferSize
            };

            GraphicsDevice.NativeDevice.CreateConstantBufferView(cbvDescription, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private IntPtr CreateShaderResourceView()
        {
            IntPtr cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

            ShaderResourceViewDescription description = new ShaderResourceViewDescription
            {
                Shader4ComponentMapping = D3DXUtilities.DefaultComponentMapping(),
                ViewDimension = ShaderResourceViewDimension.Buffer,
                Buffer =
                {
                    FirstElement = FirstElement,
                    NumElements = ElementCount,
                    StructureByteStride = StructureByteStride
                },
            };

            GraphicsDevice.NativeDevice.CreateShaderResourceView(NativeResource, description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private IntPtr CreateUnorderedAccessView()
        {
            IntPtr cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

            UnorderedAccessViewDescription description = new UnorderedAccessViewDescription
            {
                ViewDimension = UnorderedAccessViewDimension.Buffer,
                Buffer =
                {
                    FirstElement = FirstElement,
                    NumElements = ElementCount,
                    StructureByteStride = StructureByteStride
                }
            };

            GraphicsDevice.NativeDevice.CreateUnorderedAccessView(NativeResource, null, description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }

        private static ID3D12Resource CreateResource(GraphicsDevice device, GraphicsBufferDescription description)
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

        private static GraphicsBufferDescription ConvertFromNativeDescription(ResourceDescription description, GraphicsHeapType heapType)
        {
            return new GraphicsBufferDescription
            {
                SizeInBytes = (int)description.Width,
                HeapType = heapType,
                Flags = (ResourceFlags)description.Flags
            };
        }

        private static ResourceDescription ConvertToNativeDescription(GraphicsBufferDescription description)
        {
            return ResourceDescription.Buffer(description.SizeInBytes, (Vortice.Direct3D12.ResourceFlags)description.Flags);
        }
    }

    public class GraphicsBuffer<T> : GraphicsBuffer where T : unmanaged
    {
        public GraphicsBuffer(GraphicsDevice device, GraphicsBufferDescription description) : base(device, description)
        {
        }

        protected GraphicsBuffer(GraphicsDevice device, ID3D12Resource resource) : base(device, resource)
        {
        }

        public new GraphicsBuffer<T> Slice(int start, int length)
        {
            GraphicsBuffer<T> buffer = new GraphicsBuffer<T>(GraphicsDevice, NativeResource);
            buffer.InitializeFromDescription(Description, FirstElement + start, length);

            return buffer;
        }

        public T[] GetData() => GetData<T>();

        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    internal class D3DXUtilities
    {
        public const int ComponentMappingMask = 0x7;

        public const int ComponentMappingShift = 3;

        public const int ComponentMappingAlwaysSetBitAvoidingZeromemMistakes = 1 << (ComponentMappingShift * 4);

        public static int ComponentMapping(int src0, int src1, int src2, int src3)
        {
            return ((src0) & ComponentMappingMask)
                | (((src1) & ComponentMappingMask) << ComponentMappingShift)
                | (((src2) & ComponentMappingMask) << (ComponentMappingShift * 2))
                | (((src3) & ComponentMappingMask) << (ComponentMappingShift * 3))
                | ComponentMappingAlwaysSetBitAvoidingZeromemMistakes;
        }

        public static int DefaultComponentMapping()
        {
            return ComponentMapping(0, 1, 2, 3);
        }

        public static int ComponentMapping(int ComponentToExtract, int Mapping)
        {
            return (Mapping >> (ComponentMappingShift * ComponentToExtract)) & ComponentMappingMask;
        }
    }
}
