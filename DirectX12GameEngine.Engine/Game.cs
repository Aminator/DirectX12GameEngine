using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public class Game : GameBase
    {
        public Game(GameContext gameContext) : base(gameContext)
        {
            PresentationParameters presentationParameters = new PresentationParameters(
                GameWindow.ClientBounds.Width, GameWindow.ClientBounds.Height, GameWindow.NativeWindow);

            switch (Context)
            {
#if WINDOWS_UWP
                case GameContextHolographic context:
                    presentationParameters.Stereo = Windows.Graphics.Holographic.HolographicDisplay.GetDefault().IsStereo;
                    GraphicsDevice.Presenter = new HolographicGraphicsPresenter(GraphicsDevice, presentationParameters, context.HolographicSpace);
                    break;
#endif
                default:
                    GraphicsDevice.Presenter = new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
                    break;
            }

            GraphicsDevice.Presenter.ResizeViewport(
                GraphicsDevice.Presenter.PresentationParameters.BackBufferWidth,
                GraphicsDevice.Presenter.PresentationParameters.BackBufferHeight);

            Content = Services.GetRequiredService<ContentManager>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();

            GameSystems.Add(SceneSystem);
        }

        public ContentManager Content { get; }

        public GraphicsDevice GraphicsDevice { get; } = new GraphicsDevice();

        public SceneSystem SceneSystem { get; }

        public override void Dispose()
        {
            base.Dispose();

            GraphicsDevice.Dispose();
        }

        protected override void BeginDraw()
        {
            GraphicsDevice.CommandList.Reset();

            if (GraphicsDevice.Presenter != null)
            {
                int width = GameWindow.ClientBounds.Width;
                int height = GameWindow.ClientBounds.Height;

                if (width != GraphicsDevice.Presenter.Viewport.Width || height != GraphicsDevice.Presenter.Viewport.Height)
                {
#if WINDOWS_UWP
                    if (!(Context is GameContextHolographic))
#endif
                    {
                        GraphicsDevice.Presenter.Resize(width, height);
                        GraphicsDevice.Presenter.ResizeViewport(width, height);
                    }
                }

                GraphicsDevice.Presenter.BeginDraw(GraphicsDevice.CommandList);

                GraphicsDevice.CommandList.SetViewport(GraphicsDevice.Presenter.Viewport);
                GraphicsDevice.CommandList.SetScissorRectangles(GraphicsDevice.Presenter.ScissorRect);
                GraphicsDevice.CommandList.SetRenderTargets(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            }

            base.BeginDraw();
        }

        protected override void EndDraw()
        {
            base.EndDraw();

            GraphicsDevice.CommandList.Flush(true);
            GraphicsDevice.Presenter?.Present();
        }

        protected override IServiceCollection ConfigureServices()
        {
            return base.ConfigureServices()
                .AddSingleton(this)
                .AddSingleton(GraphicsDevice)
                .AddSingleton<GltfModelLoader>()
                .AddSingleton<ContentManager>()
                .AddSingleton<SceneSystem>();
        }
    }
}
