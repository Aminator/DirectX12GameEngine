using System;
using System.Numerics;
using System.Threading.Tasks;
using DirectX12GameEngine.Assets;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;

namespace DirectX12Game
{
    public sealed class MyGame : Game
    {
        public MyGame(GameContext context) : base(context)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 0;
            }
        }

        public override async Task LoadContentAsync()
        {
            // TODO: DirectX12GameEngine.Assets.dll does not get copied to the output directory if it is never used.
            MaterialAsset materialAsset = new MaterialAsset();
            materialAsset.ToString();

            SceneSystem.RootEntity = await Content.LoadAsync<Entity>(@"Assets\Scenes\Scene1");

            await base.LoadContentAsync();
        }

        public override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.CommandList.ClearRenderTargetView(GraphicsDevice.Presenter.BackBuffer, new Vector4(0.0f, 0.25f, 0.5f, 1.0f));
                GraphicsDevice.CommandList.ClearDepthStencilView(GraphicsDevice.Presenter.DepthStencilBuffer, ClearFlags.FlagsDepth);
            }
        }

        public override void EndDraw()
        {
            base.EndDraw();
        }
    }
}
