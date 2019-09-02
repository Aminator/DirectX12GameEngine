using System;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public abstract class GraphicsResource : IDisposable
    {
#nullable disable
        protected GraphicsResource()
        {
        }
#nullable restore

        protected GraphicsResource(GraphicsDevice device)
        {
            GraphicsDevice = device;
        }

        public GraphicsDevice GraphicsDevice { get; set; }

        public IntPtr MappedResource { get; private set; }

        protected internal ID3D12Resource? NativeResource { get; protected set; }

        protected internal CpuDescriptorHandle NativeRenderTargetView { get; protected set; }

        protected internal CpuDescriptorHandle NativeShaderResourceView { get; protected set; }

        protected internal CpuDescriptorHandle NativeUnorderedAccessView { get; protected set; }

        public virtual void Dispose()
        {
            NativeResource?.Dispose();
        }

        public IntPtr Map(int subresource)
        {
            IntPtr mappedResource = NativeResource?.Map(subresource) ?? throw new InvalidOperationException();
            MappedResource = mappedResource;
            return mappedResource;
        }

        public void Unmap(int subresource)
        {
            NativeResource?.Unmap(subresource);
            MappedResource = IntPtr.Zero;
        }
    }
}
