using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Engine
{
    public sealed class CameraSystem : EntitySystem<CameraComponent>
    {
        private readonly GraphicsDevice graphicsDevice;

        public CameraSystem(GraphicsDevice device) : base(typeof(TransformComponent))
        {
            Order = -10;

            graphicsDevice = device;
        }

        public GraphicsDevice? GraphicsDevice { get; set; }

        public override void Draw(GameTime gameTime)
        {
            foreach (CameraComponent cameraComponent in Components)
            {
                float? screenAspectRatio = null;

                if (graphicsDevice.CommandList.Viewports.Length > 0)
                {
                    screenAspectRatio = graphicsDevice.CommandList.Viewports[0].Width / graphicsDevice.CommandList.Viewports[0].Height;
                }

                cameraComponent.Update(screenAspectRatio);
            }
        }
    }
}
