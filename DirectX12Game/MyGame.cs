using DirectX12GameEngine.Assets;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;

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

            SceneSystem.InitialScenePath = @"Assets\Scenes\Scene1.xml";

            // TODO: DirectX12GameEngine.Assets.dll does not get copied to the output directory if it is never used.
            MaterialAsset materialAsset = new MaterialAsset(Content, GraphicsDevice);
            materialAsset.ToString();
        }

        protected override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice.Presenter != null)
            {
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.25f, 0.5f, 1.0f));
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, SharpDX.Direct3D12.ClearFlags.FlagsDepth);
            }
        }
    }
}
