using System;

namespace DirectX12GameEngine.Graphics
{
    public class DepthStencilView : ResourceView
    {
        public DepthStencilView(GraphicsResource resource) : base(resource, CreateDepthStencilView(resource))
        {
        }

        private static IntPtr CreateDepthStencilView(GraphicsResource resource)
        {
            IntPtr cpuHandle = resource.GraphicsDevice.DepthStencilViewAllocator.Allocate(1);
            resource.GraphicsDevice.NativeDevice.CreateDepthStencilView(resource.NativeResource, null, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }
    }
}
