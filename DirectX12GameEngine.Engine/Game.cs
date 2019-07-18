using System;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public class Game : GameBase
    {
        private readonly IGraphicsDeviceManager graphicsDeviceManager;

        public Game()
        {
            graphicsDeviceManager = Services.GetRequiredService<IGraphicsDeviceManager>();

            SceneSystem = Services.GetRequiredService<SceneSystem>();
            Script = Services.GetRequiredService<ScriptSystem>();
            ShaderContent = Services.GetRequiredService<ShaderContentManager>();

            GameSystems.Add(SceneSystem);
            GameSystems.Add(Script);
        }

        public GraphicsDevice? GraphicsDevice => graphicsDeviceManager.GraphicsDevice;

        public SceneSystem SceneSystem { get; }

        public ScriptSystem Script { get; }

        public ShaderContentManager ShaderContent { get; }

        public override void Dispose()
        {
            base.Dispose();

            if (graphicsDeviceManager is IDisposable disposableGraphicsDeviceManager)
            {
                disposableGraphicsDeviceManager.Dispose();
            }
        }

        protected override void Initialize()
        {
            graphicsDeviceManager.CreateDevice();

            if (Window is null || GraphicsDevice is null) return;

            PresentationParameters presentationParameters = new PresentationParameters(
                Window.ClientBounds.Width, Window.ClientBounds.Height, Window.NativeWindow);

            switch (Context)
            {
                case GameContextHolographic context:
                    presentationParameters.Stereo = Windows.Graphics.Holographic.HolographicDisplay.GetDefault().IsStereo;
                    GraphicsDevice.Presenter = new Graphics.Holographic.HolographicGraphicsPresenter(GraphicsDevice, presentationParameters, context.HolographicSpace);
                    break;
                default:
                    GraphicsDevice.Presenter = new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
                    break;
            }

            base.Initialize();
        }

        protected override void BeginDraw()
        {
            if (!graphicsDeviceManager.BeginDraw())
            {
                return;
            }

            if (GraphicsDevice != null)
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

                if (Window != null && GraphicsDevice.Presenter != null)
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
            }

            base.BeginDraw();
        }

        protected override void EndDraw()
        {
            base.EndDraw();

            GraphicsDevice?.CommandList.Flush(true);
            graphicsDeviceManager.EndDraw();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<IGraphicsDeviceManager, GraphicsDeviceManager>();
            services.AddSingleton<SceneSystem>();
            services.AddSingleton<ScriptSystem>();
            services.AddSingleton<ShaderContentManager>();
        }
    }
}
