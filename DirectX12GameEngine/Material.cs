using System.Collections.Generic;

namespace DirectX12GameEngine
{
    public class Material
    {
        public Material(GraphicsPipelineState pipelineState)
        {
            PipelineState = pipelineState;
        }

        public GraphicsPipelineState PipelineState { get; }

        public List<Texture> Textures { get; } = new List<Texture>();
    }
}
