using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
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
                    GraphicsDevice.Presenter = new Graphics.Holographic.HolographicGraphicsPresenter(GraphicsDevice, presentationParameters, context.HolographicSpace);
                    break;
#endif
                default:
                    GraphicsDevice.Presenter = new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
                    break;
            }

            SceneSystem = Services.GetRequiredService<SceneSystem>();

            GameSystems.Add(SceneSystem);
        }

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

#if WINDOWS_UWP
            if (Context is GameContextXaml xamlContext && GraphicsDevice.Presenter is SwapChainGraphicsPresenter swapChainGraphicsPresenter)
            {
                var swapChainPanel = xamlContext.Control;

                swapChainGraphicsPresenter.MatrixTransform = new System.Numerics.Matrix3x2
                {
                    M11 = 1.0f / swapChainPanel.CompositionScaleX,
                    M22 = 1.0f / swapChainPanel.CompositionScaleY
                };
            }
#endif

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

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton(GraphicsDevice);
            services.AddSingleton<SceneSystem>();
        }
    }
}
