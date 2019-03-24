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

        public CpuDescriptorHandle NativeConstantBufferCpuDescriptorHandle { get; set; }

        public GpuDescriptorHandle NativeConstantBufferGpuDescriptorHandle { get; set; }

        public CpuDescriptorHandle NativeSamplerCpuDescriptorHandle { get; set; }

        public GpuDescriptorHandle NativeSamplerGpuDescriptorHandle { get; set; }

        public CpuDescriptorHandle NativeTextureCpuDescriptorHandle { get; set; }

        public GpuDescriptorHandle NativeTextureGpuDescriptorHandle { get; set; }
    }
}
