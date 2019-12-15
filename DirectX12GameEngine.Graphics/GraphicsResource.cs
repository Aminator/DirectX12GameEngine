using System;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public abstract class GraphicsResource : IDisposable
    {
        private GraphicsDevice? graphicsDevice;
        private ID3D12Resource? nativeResource;

        protected GraphicsResource()
        {
        }

        protected GraphicsResource(GraphicsDevice device)
        {
            GraphicsDevice = device;
        }

        public GraphicsDevice GraphicsDevice { get => graphicsDevice ?? throw new InvalidOperationException(); set => graphicsDevice = value ?? throw new ArgumentNullException(); }

        public IntPtr MappedResource { get; private set; }

        protected internal ID3D12Resource NativeResource { get => nativeResource ?? throw new InvalidOperationException(); protected set => nativeResource = value ?? throw new ArgumentNullException(); }

        protected internal CpuDescriptorHandle NativeRenderTargetView { get; protected set; }

        protected internal CpuDescriptorHandle NativeShaderResourceView { get; protected set; }

        protected internal CpuDescriptorHandle NativeUnorderedAccessView { get; protected set; }

        public virtual void Dispose()
        {
            nativeResource?.Dispose();
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
