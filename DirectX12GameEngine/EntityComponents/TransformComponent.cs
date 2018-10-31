using System.Numerics;

namespace DirectX12GameEngine
{
    public sealed class TransformComponent : EntityComponent
    {
        private Vector3 position;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = Vector3.One;

        public Matrix4x4 LocalMatrix { get; private set; } = Matrix4x4.Identity;

        public Matrix4x4 WorldMatrix { get; private set; } = Matrix4x4.Identity;

        public Vector3 Position { get => position; set { position = value; UpdateWorldMatrix(); } }

        public Quaternion Rotation { get => rotation; set { rotation = value; UpdateWorldMatrix(); } }

        public Vector3 Scale { get => scale; set { scale = value; UpdateWorldMatrix(); } }

        public TransformComponent? Parent { get; set; }

        public void UpdateLocalMatrix()
        {
            LocalMatrix = Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(position);
        }

        public void UpdateWorldMatrix()
        {
            UpdateLocalMatrix();
            WorldMatrix = LocalMatrix;
        }
    }
}
