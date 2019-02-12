using SharpDX.Direct3D12;

using PipelineState = DirectX12GameEngine.Graphics.PipelineState;

namespace DirectX12GameEngine.Rendering
{
    public class MaterialPass
    {
        public int PassIndex { get; set; }

#nullable disable
        public PipelineState PipelineState { get; set; }

        internal CpuDescriptorHandle NativeCpuDescriptorHandle { get; set; }

        internal GpuDescriptorHandle NativeGpuDescriptorHandle { get; set; }
#nullable enable
    }
}
