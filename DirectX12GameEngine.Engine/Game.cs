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
                Window.ClientBounds.Width, Window.ClientBounds.Height, Window.NativeWindow);

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
                int width = Window.ClientBounds.Width;
                int height = Window.ClientBounds.Height;

                if (width != GraphicsDevice.Presenter.BackBuffer.Width || height != GraphicsDevice.Presenter.BackBuffer.Height)
                {
#if WINDOWS_UWP
                    if (!(Context is GameContextHolographic))
#endif
                    {
                        GraphicsDevice.Presenter.Resize(width, height);
                    }
                }
            }

            GraphicsDevice.CommandList.ClearState();

            GraphicsDevice.Presenter?.BeginDraw(GraphicsDevice.CommandList);

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
                .AddSingleton(GraphicsDevice)
                .AddSingleton<GltfModelLoader>()
                .AddSingleton<ContentManager>()
                .AddSingleton<SceneSystem>();
        }
    }
}
