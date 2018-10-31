using DirectX12GameEngine;

[module: System.Runtime.CompilerServices.NonNullTypes]
namespace DirectX12Game
{
    public sealed class MyGame : Game
    {
        public MyGame(GameContext gameContext) : base(gameContext)
        {
            if (GraphicsDevice.Presenter != null)
            {
                GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
            }

            SceneSystem.InitialScenePath = "Scene1.xml";
        }

        protected override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice.Presenter != null)
            {
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.5f, 0.5f, 1.0f));
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, SharpDX.Direct3D12.ClearFlags.FlagsDepth);
            }
        }
    }
}
