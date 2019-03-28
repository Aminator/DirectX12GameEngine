using System.Numerics;

namespace DirectX12GameEngine.Rendering
{
    public sealed class Mesh
    {
        public Mesh(MeshDraw meshDraw)
        {
            MeshDraw = meshDraw;
        }

        public int MaterialIndex { get; set; }

        public MeshDraw MeshDraw { get; set; }

        public Matrix4x4 WorldMatrix { get; set; } = Matrix4x4.Identity;
    }
}
