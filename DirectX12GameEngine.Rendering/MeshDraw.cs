using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Rendering
{
    public sealed class MeshDraw
    {
        public GraphicsBuffer? IndexBufferView { get; set; }

        public GraphicsBuffer[]? VertexBufferViews { get; set; }
    }
}
