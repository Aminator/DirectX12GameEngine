using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Rendering
{
    public sealed class MeshDraw
    {
        public IndexBufferView? IndexBufferView { get; set; }

        public VertexBufferView[]? VertexBufferViews { get; set; }
    }
}
