using System;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class RenderTargetView : ResourceView
    {
        public RenderTargetView(GraphicsResource resource) : this(resource, null)
        {
        }

        internal RenderTargetView(GraphicsResource resource, RenderTargetViewDescription? description) : base(resource, CreateRenderTargetView(resource, description))
        {
            Description = description;
        }

        internal RenderTargetViewDescription? Description { get; }

        public static RenderTargetView FromTexture2D(GraphicsResource resource, PixelFormat format)
        {
            return new RenderTargetView(resource, new RenderTargetViewDescription
            {
                ViewDimension = RenderTargetViewDimension.Texture2D,
                Format = (Format)format
            });
        }

        private static IntPtr CreateRenderTargetView(GraphicsResource resource, RenderTargetViewDescription? description)
        {
            IntPtr cpuHandle = resource.GraphicsDevice.RenderTargetViewAllocator.Allocate(1);
            resource.GraphicsDevice.NativeDevice.CreateRenderTargetView(resource.NativeResource, description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }
    }
}
