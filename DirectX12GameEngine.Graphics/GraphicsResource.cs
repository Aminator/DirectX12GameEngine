using System;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public abstract class GraphicsResource : IDisposable
    {
        protected GraphicsResource(GraphicsDevice device, ID3D12Resource resource)
        {
            GraphicsDevice = device;
            NativeResource = resource;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public ID3D12Resource NativeResource { get; }

        public IntPtr MappedResource { get; private set; }

        public abstract IntPtr NativeRenderTargetView { get; }

        public abstract IntPtr NativeShaderResourceView { get; }

        public abstract IntPtr NativeUnorderedAccessView { get; }

        public abstract IntPtr NativeConstantBufferView { get; }

        public abstract IntPtr NativeDepthStencilView { get; }

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
