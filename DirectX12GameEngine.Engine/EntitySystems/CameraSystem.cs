using System;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public sealed class CameraSystem : EntitySystem<CameraComponent>
    {
        public CameraSystem(IServiceProvider services) : base(services, typeof(TransformComponent))
        {
            Order = -10;

            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
        }

        public GraphicsDevice GraphicsDevice { get; }

        public override void Draw(GameTime gameTime)
        {
            foreach (CameraComponent cameraComponent in Components)
            {
                float screenAspectRatio = GraphicsDevice.CommandList.Viewports[0].Width / GraphicsDevice.CommandList.Viewports[0].Height;

                cameraComponent.Update(screenAspectRatio);
            }
        }
    }
}
