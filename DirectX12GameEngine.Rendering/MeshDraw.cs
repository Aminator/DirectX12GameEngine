using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Rendering
{
    public sealed class MeshDraw
    {
        public IndexBufferView? IndexBufferView { get; set; }

        public VertexBufferView[]? VertexBufferViews { get; set; }
    }
}
