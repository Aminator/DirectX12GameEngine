using ShaderGen;

namespace DirectX12GameEngine
{
    public class SystemInstanceIdSemanticAttribute : VertexSemanticAttribute
    {
        public SystemInstanceIdSemanticAttribute() : base(SemanticType.None)
        {
        }
    }

    public class SystemRenderTargetArrayIndexSemanticAttribute : VertexSemanticAttribute
    {
        public SystemRenderTargetArrayIndexSemanticAttribute() : base(SemanticType.None)
        {
        }
    }
}
