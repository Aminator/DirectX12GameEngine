using System;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class ConstantBufferView : ResourceView
    {
        public ConstantBufferView(GraphicsResource resource) : base(resource, CreateConstantBufferView(resource))
        {
        }

        public ConstantBufferView(ConstantBufferView constantBufferView) : base(constantBufferView.Resource, constantBufferView.CpuDescriptorHandle)
        {
        }

        private static IntPtr CreateConstantBufferView(GraphicsResource resource)
        {
            IntPtr cpuHandle = resource.GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);

            int constantBufferSize = ((int)resource.SizeInBytes + 255) & ~255;

            ConstantBufferViewDescription cbvDescription = new ConstantBufferViewDescription
            {
                BufferLocation = resource.NativeResource.GPUVirtualAddress,
                SizeInBytes = constantBufferSize
            };

            resource.GraphicsDevice.NativeDevice.CreateConstantBufferView(cbvDescription, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }
    }
}
