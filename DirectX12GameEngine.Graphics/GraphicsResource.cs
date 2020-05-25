using System;
using System.Runtime.CompilerServices;
using DirectX12GameEngine.Core;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class GraphicsResource : IDisposable
    {
        ConstantBufferView? defaultConstantBufferView;

        public GraphicsResource(GraphicsDevice device, ResourceDescription description, HeapType heapType) : this(device, CreateResource(device, description, heapType))
        {
        }

        internal GraphicsResource(GraphicsDevice device, ID3D12Resource resource)
        {
            GraphicsDevice = device;
            NativeResource = resource;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public ResourceDescription Description => Unsafe.As<Vortice.Direct3D12.ResourceDescription, ResourceDescription>(ref Unsafe.AsRef(NativeResource.Description));

        public ResourceDimension Dimension => Description.Dimension;

        public HeapType HeapType => (HeapType)NativeResource.HeapProperties.Type;

        public long Width => Description.Width;

        public int Height => Description.Height;

        public short DepthOrArraySize => Description.DepthOrArraySize;

        public long SizeInBytes => Description.Width * Description.Height * Description.DepthOrArraySize;

        public PixelFormat Format => Description.Format;

        public ResourceFlags Flags => Description.Flags;

        public IntPtr MappedResource { get; private set; }

        public ConstantBufferView DefaultConstantBufferView => defaultConstantBufferView ??= new ConstantBufferView(this);

        internal ID3D12Resource NativeResource { get; }

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

        #region Creation Methods

        public static GraphicsResource CreateBuffer(GraphicsDevice device, int size, ResourceFlags bufferFlags, HeapType heapType = HeapType.Default)
        {
            return new GraphicsResource(device, ResourceDescription.Buffer(size, bufferFlags), heapType);
        }

        public static GraphicsResource CreateBuffer<T>(GraphicsDevice device, int elementCount, ResourceFlags bufferFlags, HeapType heapType = HeapType.Default) where T : unmanaged
        {
            return CreateBuffer(device, elementCount * Unsafe.SizeOf<T>(), bufferFlags, heapType);
        }

        public static unsafe GraphicsResource CreateBuffer<T>(GraphicsDevice device, in T data, ResourceFlags bufferFlags, HeapType heapType = HeapType.Default) where T : unmanaged
        {
            fixed (T* pointer = &data)
            {
                return CreateBuffer(device, new Span<T>(pointer, 1), bufferFlags, heapType);
            }
        }

        public static GraphicsResource CreateBuffer<T>(GraphicsDevice device, Span<T> data, ResourceFlags bufferFlags, HeapType heapType = HeapType.Default) where T : unmanaged
        {
            GraphicsResource buffer = CreateBuffer<T>(device, data.Length, bufferFlags, heapType);
            buffer.SetData(data);

            return buffer;
        }

        #endregion

        private static ID3D12Resource CreateResource(GraphicsDevice device, ResourceDescription description, HeapType heapType)
        {
            ResourceStates resourceStates = ResourceStates.Common;

            if (heapType == HeapType.Upload)
            {
                resourceStates = ResourceStates.GenericRead;
            }
            else if (heapType == HeapType.Readback)
            {
                resourceStates = ResourceStates.CopyDestination;
            }

            return device.NativeDevice.CreateCommittedResource(
                new HeapProperties((Vortice.Direct3D12.HeapType)heapType), HeapFlags.None,
                Unsafe.As<ResourceDescription, Vortice.Direct3D12.ResourceDescription>(ref description), resourceStates);
        }

        public T[] GetArray<T>(int offsetInBytes = 0) where T : unmanaged
        {
            T[] data = new T[(SizeInBytes / Unsafe.SizeOf<T>()) - offsetInBytes];
            GetData(data.AsSpan(), offsetInBytes);

            return data;
        }

        public T GetData<T>(int offsetInBytes = 0) where T : unmanaged
        {
            T data = new T();
            GetData(ref data, offsetInBytes);

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
            if (Dimension == ResourceDimension.Buffer)
            {
                if (HeapType == HeapType.Default)
                {
                    using GraphicsResource readbackBuffer = CreateBuffer<T>(GraphicsDevice, data.Length, ResourceFlags.None, HeapType.Readback);
                    using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                    copyCommandList.CopyBufferRegion(this, offsetInBytes, readbackBuffer, 0, data.Length * Unsafe.SizeOf<T>());
                    copyCommandList.Flush();

                    readbackBuffer.GetData(data);
                }
                else
                {
                    Map(0);
                    IntPtr source = MappedResource + offsetInBytes;
                    source.CopyTo(data);
                    Unmap(0);
                }
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
            if (Dimension == ResourceDimension.Buffer)
            {
                if (HeapType == HeapType.Default)
                {
                    using GraphicsResource uploadBuffer = CreateBuffer(GraphicsDevice, data, ResourceFlags.None, HeapType.Upload);
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
            else if (Dimension == ResourceDimension.Texture2D)
            {
                ID3D12Resource uploadResource = GraphicsDevice.NativeDevice.CreateCommittedResource(new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), HeapFlags.None, NativeResource.Description, ResourceStates.CopyDestination);
                using Texture textureUploadBuffer = new Texture(GraphicsDevice, uploadResource);

                textureUploadBuffer.NativeResource.WriteToSubresource(0, data, (int)Width * 4, (int)Width * Height * 4);

                using CommandList copyCommandList = new CommandList(GraphicsDevice, CommandListType.Copy);

                copyCommandList.CopyResource(textureUploadBuffer, this);
                copyCommandList.Flush();
            }
        }
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
