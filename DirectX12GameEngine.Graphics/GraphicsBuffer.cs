using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DirectX12GameEngine.Core;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public partial class GraphicsBuffer : GraphicsResource
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GraphicsBuffer()
        {
        }

        protected internal GraphicsBuffer(GraphicsDevice device) : base(device)
        {
        }

        public GraphicsBufferDescription Description { get; private set; }

        public int SizeInBytes => Description.SizeInBytes;

        public GraphicsBufferFlags Flags => Description.Flags;

        public GraphicsHeapType HeapType => Description.HeapType;

        public int StructureByteStride => Description.StructureByteStride;

        public int FirstElement { get; protected set; }

        public int ElementCount { get; protected set; }

        internal CpuDescriptorHandle NativeConstantBufferView { get; private set; }

        public static GraphicsBuffer New(GraphicsDevice device, GraphicsBufferDescription description)
        {
            return new GraphicsBuffer(device).InitializeFrom(description);
        }

        public static GraphicsBuffer New(GraphicsDevice device, int size, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return New(device, new GraphicsBufferDescription(size, bufferFlags, heapType));
        }

        public static GraphicsBuffer New(GraphicsDevice device, int size, int structureByteStride, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return New(device, new GraphicsBufferDescription(size, bufferFlags, heapType, structureByteStride));
        }

        public static GraphicsBuffer<T> New<T>(GraphicsDevice device, int elementCount, int structureByteStride, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            int size = structureByteStride * elementCount;

            return new GraphicsBuffer<T>(device, new GraphicsBufferDescription(size, bufferFlags, heapType, structureByteStride));
        }

        public static GraphicsBuffer<T> New<T>(GraphicsDevice device, int elementCount, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            return New<T>(device, elementCount, Unsafe.SizeOf<T>(), bufferFlags, heapType);
        }

        public static unsafe GraphicsBuffer<T> New<T>(GraphicsDevice device, in T data, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                return New(device, new Span<T>(pointer, 1), bufferFlags, heapType);
            }
        }

        public static GraphicsBuffer<T> New<T>(GraphicsDevice device, Span<T> data, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            return New(device, data, Unsafe.SizeOf<T>(), bufferFlags, heapType);
        }

        public static GraphicsBuffer<T> New<T>(GraphicsDevice device, Span<T> data, int structureByteStride, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType = GraphicsHeapType.Default) where T : unmanaged
        {
            GraphicsBuffer<T> buffer = New<T>(device, data.Length, structureByteStride, bufferFlags, heapType);
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
            offsetInBytes = FirstElement * StructureByteStride + offsetInBytes;

            if (HeapType == GraphicsHeapType.Default)
            {
                using GraphicsBuffer<T> readbackBaffer = New<T>(GraphicsDevice, data.Length, GraphicsBufferFlags.None, GraphicsHeapType.Readback);
                using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                copyCommandList.CopyBufferRegion(this, offsetInBytes, readbackBaffer, 0, data.Length * Unsafe.SizeOf<T>());
                copyCommandList.Flush(true);

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
                using GraphicsBuffer<T> uploadBuffer = New(GraphicsDevice, data, GraphicsBufferFlags.None, GraphicsHeapType.Upload);
                using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                copyCommandList.CopyBufferRegion(uploadBuffer, 0, this, offsetInBytes, data.Length * Unsafe.SizeOf<T>());
                copyCommandList.Flush(true);
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
            return new GraphicsBuffer(GraphicsDevice).InitializeFrom(NativeResource!, Description, FirstElement + start, length);
        }

        public GraphicsBuffer InitializeFrom(GraphicsBufferDescription description)
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

        protected internal GraphicsBuffer InitializeFrom(ID3D12Resource resource, bool isShaderResource = false)
        {
            resource.GetHeapProperties(out HeapProperties heapProperties, out _);

            GraphicsBufferDescription description = ConvertFromNativeDescription(resource.Description, (GraphicsHeapType)heapProperties.Type, isShaderResource);

            return InitializeFrom(resource, description);
        }

        protected internal GraphicsBuffer InitializeFrom(ID3D12Resource resource, GraphicsBufferDescription description, int firstElement = 0, int elementCount = 0)
        {
            NativeResource = resource;
            Description = description;

            FirstElement = firstElement;

            if (description.StructureByteStride != 0)
            {
                ElementCount = elementCount == 0 ? description.SizeInBytes / description.StructureByteStride : elementCount;
            }

            if (description.Flags.HasFlag(GraphicsBufferFlags.RenderTarget))
            {
                NativeRenderTargetView = CreateRenderTargetView();
            }

            if (description.Flags.HasFlag(GraphicsBufferFlags.ConstantBuffer))
            {
                NativeConstantBufferView = CreateConstantBufferView();
            }

            if (description.Flags.HasFlag(GraphicsBufferFlags.ShaderResource))
            {
                NativeShaderResourceView = CreateShaderResourceView();
            }

            if (description.Flags.HasFlag(GraphicsBufferFlags.UnorderedAccess))
            {
                NativeUnorderedAccessView = CreateUnorderedAccessView();
            }

            return this;
        }

        private static GraphicsBufferDescription ConvertFromNativeDescription(ResourceDescription description, GraphicsHeapType heapType, bool isShaderResource = false)
        {
            GraphicsBufferFlags flags = GraphicsBufferFlags.None;

            if (description.Flags.HasFlag(ResourceFlags.AllowUnorderedAccess))
            {
                flags |= GraphicsBufferFlags.UnorderedAccess;
            }

            if (!description.Flags.HasFlag(ResourceFlags.DenyShaderResource) && isShaderResource)
            {
                flags |= GraphicsBufferFlags.ShaderResource;
            }

            return new GraphicsBufferDescription
            {
                SizeInBytes = (int)description.Width,
                HeapType = heapType,
                Flags = flags
            };
        }

        private static ResourceDescription ConvertToNativeDescription(GraphicsBufferDescription description)
        {
            ResourceFlags flags = ResourceFlags.None;

            if (description.Flags.HasFlag(GraphicsBufferFlags.RenderTarget))
            {
                flags |= ResourceFlags.AllowRenderTarget;
            }

            if (description.Flags.HasFlag(GraphicsBufferFlags.UnorderedAccess))
            {
                flags |= ResourceFlags.AllowUnorderedAccess;
            }

            return ResourceDescription.Buffer(description.SizeInBytes, flags);
        }

        protected internal CpuDescriptorHandle CreateRenderTargetView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.RenderTargetViewAllocator.Allocate(1);

            RenderTargetViewDescription description = new RenderTargetViewDescription
            {
                ViewDimension = RenderTargetViewDimension.Buffer,
                Buffer =
                {
                    FirstElement = FirstElement,
                    NumElements = ElementCount
                }
            };

            GraphicsDevice.NativeDevice.CreateRenderTargetView(NativeResource, description, cpuHandle);

            return cpuHandle;
        }

        protected internal CpuDescriptorHandle CreateConstantBufferView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

            int constantBufferSize = (SizeInBytes + 255) & ~255;

            ConstantBufferViewDescription cbvDescription = new ConstantBufferViewDescription
            {
                BufferLocation = NativeResource!.GPUVirtualAddress,
                SizeInBytes = constantBufferSize
            };

            GraphicsDevice.NativeDevice.CreateConstantBufferView(cbvDescription, cpuHandle);

            return cpuHandle;
        }

        protected internal CpuDescriptorHandle CreateShaderResourceView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

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

            GraphicsDevice.NativeDevice.CreateShaderResourceView(NativeResource, description, cpuHandle);

            return cpuHandle;
        }

        protected internal CpuDescriptorHandle CreateUnorderedAccessView()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

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

            GraphicsDevice.NativeDevice.CreateUnorderedAccessView(NativeResource, null, description, cpuHandle);

            return cpuHandle;
        }
    }

    public class GraphicsBuffer<T> : GraphicsBuffer where T : unmanaged
    {
        protected internal GraphicsBuffer(GraphicsDevice device) : base(device)
        {
        }

        public GraphicsBuffer(GraphicsDevice device, GraphicsBufferDescription description) : base(device)
        {
            InitializeFrom(description);
        }

        public new GraphicsBuffer<T> Slice(int start, int length)
        {
            GraphicsBuffer<T> buffer = new GraphicsBuffer<T>(GraphicsDevice);
            buffer.InitializeFrom(NativeResource!, Description, FirstElement + start, length);

            return buffer;
        }

        public T[] GetData() => GetData<T>();
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
