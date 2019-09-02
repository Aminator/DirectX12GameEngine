using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class SamplerState
    {
        public SamplerState(GraphicsDevice device, SamplerDescription description)
        {
            GraphicsDevice = device;
            Description = description;
            NativeCpuDescriptorHandle = CreateSampler();
        }

        public GraphicsDevice GraphicsDevice { get; }

        public SamplerDescription Description { get; }

        internal CpuDescriptorHandle NativeCpuDescriptorHandle { get; }

        private CpuDescriptorHandle CreateSampler()
        {
            CpuDescriptorHandle cpuHandle = GraphicsDevice.SamplerAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateSampler(Description, cpuHandle);

            return cpuHandle;
        }
    }
}
