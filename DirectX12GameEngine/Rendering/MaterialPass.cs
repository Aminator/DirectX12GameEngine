using SharpDX.Direct3D12;

using PipelineState = DirectX12GameEngine.Graphics.PipelineState;

namespace DirectX12GameEngine.Rendering
{
    public class MaterialPass
    {
        public int PassIndex { get; set; }

#nullable disable
        public PipelineState PipelineState { get; set; }
#nullable enable

        internal CpuDescriptorHandle NativeConstantBufferCpuDescriptorHandle { get; set; }

        internal GpuDescriptorHandle NativeConstantBufferGpuDescriptorHandle { get; set; }

        internal CpuDescriptorHandle NativeSamplerCpuDescriptorHandle { get; set; }

        internal GpuDescriptorHandle NativeSamplerGpuDescriptorHandle { get; set; }

        internal CpuDescriptorHandle NativeTextureCpuDescriptorHandle { get; set; }

        internal GpuDescriptorHandle NativeTextureGpuDescriptorHandle { get; set; }
    }
}
