using System;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class Sampler
    {
        public Sampler(GraphicsDevice device) : this(device, SamplerDescription.Default)
        {
        }

        public Sampler(Sampler sampler)
        {
            GraphicsDevice = sampler.GraphicsDevice;
            Description = sampler.Description;
            CpuDescriptorHandle = sampler.CpuDescriptorHandle;
        }

        public Sampler(GraphicsDevice device, SamplerDescription description)
        {
            GraphicsDevice = device;
            Description = description;
            CpuDescriptorHandle = CreateSampler();
        }

        public GraphicsDevice GraphicsDevice { get; }

        public SamplerDescription Description { get; }

        public IntPtr CpuDescriptorHandle { get; }

        private IntPtr CreateSampler()
        {
            IntPtr cpuHandle = GraphicsDevice.SamplerAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateSampler(Description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }
    }
}
