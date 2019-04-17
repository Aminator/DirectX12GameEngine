using System;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public abstract class GraphicsResource : IDisposable
    {
        protected GraphicsResource(GraphicsDevice device, Resource resource)
        {
            GraphicsDevice = device;
            NativeResource = resource;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public IntPtr MappedResource { get; private set; }

        protected internal Resource NativeResource { get; }

        protected internal CpuDescriptorHandle NativeCpuDescriptorHandle { get; protected set; }

        protected internal GpuDescriptorHandle NativeGpuDescriptorHandle { get; protected set; }

        public virtual void Dispose()
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
