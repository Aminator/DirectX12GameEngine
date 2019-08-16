using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Rendering
{
    public sealed class MeshDraw
    {
        public Buffer? IndexBufferView { get; set; }

        public Buffer[]? VertexBufferViews { get; set; }
    }
}
