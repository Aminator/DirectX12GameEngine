using System.Numerics;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Games
{
    public class EditorGame : Game
    {
        public EditorGame(GameContext context) : base(context)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
            }
        }

        protected override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.CommandList.ClearRenderTargetView(GraphicsDevice.Presenter.BackBuffer, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                GraphicsDevice.CommandList.ClearDepthStencilView(GraphicsDevice.Presenter.DepthStencilBuffer, ClearFlags.FlagsDepth);
            }
        }
    }
}
