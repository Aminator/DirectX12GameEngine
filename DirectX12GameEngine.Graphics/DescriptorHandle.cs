using Vortice.DirectX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public readonly struct DescriptorHandle
    {
        internal DescriptorHandle(CpuDescriptorHandle cpuDescriptorHandle, GpuDescriptorHandle gpuDescriptorHandle)
        {
            CpuDescriptorHandle = cpuDescriptorHandle;
            GpuDescriptorHandle = gpuDescriptorHandle;
        }

        internal CpuDescriptorHandle CpuDescriptorHandle { get; }

        internal GpuDescriptorHandle GpuDescriptorHandle { get; }
    }
}
