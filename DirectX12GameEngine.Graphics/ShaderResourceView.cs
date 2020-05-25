using System;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class ShaderResourceView : ResourceView
    {
        public ShaderResourceView(GraphicsResource resource) : this(resource, null)
        {
        }

        public ShaderResourceView(ShaderResourceView shaderResourceView) : base(shaderResourceView.Resource, shaderResourceView.CpuDescriptorHandle)
        {
            Description = shaderResourceView.Description;
        }

        internal ShaderResourceView(GraphicsResource resource, ShaderResourceViewDescription? description) : base(resource, CreateShaderResourceView(resource, description))
        {
            Description = description;
        }

        internal ShaderResourceViewDescription? Description { get; }

        public static ShaderResourceView FromBuffer<T>(GraphicsResource resource, long firstElement = 0, int elementCount = 0) where T : unmanaged
        {
            return FromBuffer(resource, firstElement, elementCount == 0 ? (int)resource.Width / Unsafe.SizeOf<T>() : elementCount, Unsafe.SizeOf<T>());
        }

        public static ShaderResourceView FromBuffer(GraphicsResource resource, long firstElement, int elementCount, int structureByteStride)
        {
            return new ShaderResourceView(resource, new ShaderResourceViewDescription
            {
                Shader4ComponentMapping = D3DXUtilities.DefaultComponentMapping(),
                ViewDimension = ShaderResourceViewDimension.Buffer,
                Buffer =
                {
                    FirstElement = firstElement,
                    NumElements = elementCount,
                    StructureByteStride = structureByteStride
                }
            });
        }

        public static ShaderResourceView FromTexture2D(GraphicsResource resource, PixelFormat format)
        {
            return new ShaderResourceView(resource, new ShaderResourceViewDescription
            {
                Shader4ComponentMapping = D3DXUtilities.DefaultComponentMapping(),
                ViewDimension = ShaderResourceViewDimension.Texture2D,
                Format = (Format)format,
                Texture2D =
                {
                    MipLevels = resource.Description.MipLevels
                }
            });
        }

        private static IntPtr CreateShaderResourceView(GraphicsResource resource, ShaderResourceViewDescription? description)
        {
            IntPtr cpuHandle = resource.GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
            resource.GraphicsDevice.NativeDevice.CreateShaderResourceView(resource.NativeResource, description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }
    }
}
