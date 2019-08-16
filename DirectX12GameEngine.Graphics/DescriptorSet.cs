using System;
using System.Collections.Generic;
using System.Linq;
using Vortice.DirectX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class DescriptorSet
    {
        public DescriptorSet(GraphicsDevice device)
        {
            GraphicsDevice = device;
            DescriptorAllocator = GraphicsDevice.ShaderResourceViewAllocator;
        }

        public DescriptorSet(GraphicsDevice device, IEnumerable<GraphicsResource> resources) : this(device)
        {
            int resourceCount = resources.Count();

            if (resourceCount < 1) throw new ArgumentOutOfRangeException(nameof(resources));

            DescriptorCount = resources.Count();
            NativeCpuDescriptorHandle = GraphicsDevice.CopyDescriptorsToOneDescriptorHandle(DescriptorAllocator, resources);
        }

        public GraphicsDevice GraphicsDevice { get; }

        public int DescriptorCount { get; private set; }

        internal DescriptorAllocator DescriptorAllocator { get; }

        internal CpuDescriptorHandle NativeCpuDescriptorHandle { get; private set; }
    }
}
