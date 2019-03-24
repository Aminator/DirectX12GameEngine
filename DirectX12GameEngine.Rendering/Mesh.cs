using System.Numerics;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Rendering
{
    public sealed class Mesh
    {
        public IndexBufferView? IndexBufferView { get; set; }

        public int MaterialIndex { get; set; }

        public VertexBufferView[]? VertexBufferViews { get; set; }

        public Matrix4x4 WorldMatrix { get; set; } = Matrix4x4.Identity;
    }
}
