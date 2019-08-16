using DirectX12GameEngine.Graphics;
using PipelineState = DirectX12GameEngine.Graphics.PipelineState;

namespace DirectX12GameEngine.Rendering
{
    public class MaterialPass
    {
        public int PassIndex { get; set; }

        public PipelineState? PipelineState { get; set; }

        public DescriptorSet? ConstantBufferDescriptorSet { get; set; }

        public DescriptorSet? SamplerDescriptorSet { get; set; }

        public DescriptorSet? TextureDescriptorSet { get; set; }
    }
}
