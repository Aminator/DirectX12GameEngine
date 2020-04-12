using System;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public class SamplerState
    {
        IntPtr nativeCpuDescriptorHandle;

        public SamplerState(GraphicsDevice device) : this(device, SamplerDescription.Default)
        {
        }

        public SamplerState(GraphicsDevice device, SamplerDescription description)
        {
            GraphicsDevice = device;
            Description = description;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public SamplerDescription Description { get; }

        public IntPtr NativeCpuDescriptorHandle => nativeCpuDescriptorHandle = nativeCpuDescriptorHandle != IntPtr.Zero ? nativeCpuDescriptorHandle : CreateSampler();

        private IntPtr CreateSampler()
        {
            IntPtr cpuHandle = GraphicsDevice.SamplerAllocator.Allocate(1);
            GraphicsDevice.NativeDevice.CreateSampler(Description, cpuHandle.ToCpuDescriptorHandle());

            return cpuHandle;
        }
    }
}
