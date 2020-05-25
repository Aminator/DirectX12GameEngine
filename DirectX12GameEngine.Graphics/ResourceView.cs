using System;

namespace DirectX12GameEngine.Graphics
{
    public abstract class ResourceView
    {
        protected ResourceView(GraphicsResource resource, IntPtr descriptor)
        {
            Resource = resource;
            CpuDescriptorHandle = descriptor;
        }

        public GraphicsResource Resource { get; }

        internal IntPtr CpuDescriptorHandle { get; }
    }
}
