using System;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class UnorderedAccessView : ResourceView
    {
        public UnorderedAccessView(GraphicsResource resource) : this(resource, null)
        {
        }

        public UnorderedAccessView(UnorderedAccessView unorderedAccessView) : base(unorderedAccessView.Resource, unorderedAccessView.CpuDescriptorHandle)
        {
            Description = unorderedAccessView.Description;
        }

        internal UnorderedAccessView(GraphicsResource resource, UnorderedAccessViewDescription? description) : base(resource, CreateUnorderedAccessView(resource, description))
        {
            Description = description;
        }

        internal UnorderedAccessViewDescription? Description { get; }

        public static UnorderedAccessView FromBuffer<T>(GraphicsResource resource, long firstElement = 0, int elementCount = 0) where T : unmanaged
        {
            return FromBuffer(resource, firstElement, elementCount == 0 ? (int)resource.Width / Unsafe.SizeOf<T>() : elementCount, Unsafe.SizeOf<T>());
        }

        public static UnorderedAccessView FromBuffer(GraphicsResource resource, long firstElement, int elementCount, int structureByteStride)
        {
            return new UnorderedAccessView(resource, new UnorderedAccessViewDescription
            {
                ViewDimension = UnorderedAccessViewDimension.Buffer,
                Buffer =
                {
                    FirstElement = firstElement,
                    NumElements = elementCount,
                    StructureByteStride = structureByteStride
                }
            });
        }

        public static UnorderedAccessView FromTexture2D(GraphicsResource resource, PixelFormat format)
        {
            return new UnorderedAccessView(resource, new UnorderedAccessViewDescription
            {
                ViewDimension = UnorderedAccessViewDimension.Texture2D,
                Format = (Format)format
            });
        }

        private static IntPtr CreateUnorderedAccessView(GraphicsResource resource, UnorderedAccessViewDescription? description)
        {
            IntPtr cpuHandle = resource.GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            resource.GraphicsDevice.NativeDevice.CreateUnorderedAccessView(resource.NativeResource, null, description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }
    }
}
