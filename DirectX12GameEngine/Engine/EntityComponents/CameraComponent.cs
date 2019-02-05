using System.Numerics;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(CameraSystem))]
    public sealed class CameraComponent : EntityComponent
    {
        public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;

        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;

        public Matrix4x4 ViewProjectionMatrix { get; set; } = Matrix4x4.Identity;

        public float FieldOfView { get; set; } = 60.0f;

        public float FarPlaneDistance { get; set; } = 100000.0f;

        public float NearPlaneDistance { get; set; } = 0.1f;
    }
}
