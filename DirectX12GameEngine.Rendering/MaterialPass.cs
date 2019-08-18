using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Rendering
{
    public class MaterialPass
    {
        public int PassIndex { get; set; }

        public PipelineState? PipelineState { get; set; }

        public DescriptorSet? ShaderResourceDescriptorSet { get; set; }

        public DescriptorSet? SamplerDescriptorSet { get; set; }
    }
}
