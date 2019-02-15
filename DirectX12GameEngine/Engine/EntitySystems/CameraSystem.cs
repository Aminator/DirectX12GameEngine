using System;
using System.Numerics;

namespace DirectX12GameEngine.Engine
{
    public sealed class CameraSystem : EntitySystem<CameraComponent>
    {
        public CameraSystem(IServiceProvider services) : base(services, typeof(TransformComponent))
        {
        }

        public override void Update(TimeSpan deltaTime)
        {
            if (GraphicsDevice.Presenter is null) return;

            foreach (CameraComponent cameraComponent in Components)
            {
                if (cameraComponent.Entity is null) continue;

                Matrix4x4.Decompose(cameraComponent.Entity.Transform.WorldMatrix, out _,
                    out Quaternion rotation,
                    out Vector3 translation);

                Vector3 forwardVector = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, rotation));
                Vector3 upVector = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rotation));
                cameraComponent.ViewMatrix = Matrix4x4.CreateLookAt(translation, translation + forwardVector, upVector);

                cameraComponent.ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                    cameraComponent.FieldOfView * ((float)Math.PI / 180.0f),
                    GraphicsDevice.Presenter.Viewport.Width / GraphicsDevice.Presenter.Viewport.Height,
                    cameraComponent.NearPlaneDistance,
                    cameraComponent.FarPlaneDistance);

                cameraComponent.ViewProjectionMatrix = cameraComponent.ViewMatrix * cameraComponent.ProjectionMatrix;
            }
        }
    }
}
