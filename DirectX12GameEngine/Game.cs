using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine
{
    public class Game : IDisposable
    {
        private DateTime previousTime;

        public Game(GameContext gameContext)
        {
            GameContext = gameContext;

            GraphicsDevice = new GraphicsDevice(SharpDX.Direct3D.FeatureLevel.Level_11_0);

            PresentationParameters presentationParameters = new PresentationParameters(
                gameContext.RequestedWidth, gameContext.RequestedHeight, gameContext);

            switch (GameContext)
            {
                case GameContextHolographic context:
                    presentationParameters.Stereo = Windows.Graphics.Holographic.HolographicDisplay.GetDefault().IsStereo;
                    GraphicsDevice.Presenter = new HolographicGraphicsPresenter(GraphicsDevice, presentationParameters);
                    break;
                default:
                    GraphicsDevice.Presenter = new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
                    break;
            }

            GraphicsDevice.Presenter.ResizeViewport(
                GraphicsDevice.Presenter.PresentationParameters.BackBufferWidth,
                GraphicsDevice.Presenter.PresentationParameters.BackBufferHeight);

            Services = ConfigureServices();

            Content = Services.GetRequiredService<ContentManager>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();

            GameSystems.AddRange(new List<GameSystem>
            {
                SceneSystem
            });
        }

        public ContentManager Content { get; }

        public GameContext GameContext { get; }

        public List<GameSystem> GameSystems { get; } = new List<GameSystem>();

        public GraphicsDevice GraphicsDevice { get; }

        public SceneSystem SceneSystem { get; }

        public IServiceProvider Services { get; }

        public virtual void Dispose()
        {
            GraphicsDevice.Dispose();

            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Dispose();
            }
        }

        public void Run()
        {
            Initialize();
            LoadContentAsync();

            previousTime = DateTime.Now;

            switch (GameContext)
            {
                case GameContextXaml context:
                    Windows.UI.Xaml.Media.CompositionTarget.Rendering += (s, e) => Tick();
                    return;
#if NETCOREAPP
                case GameContextWinForms context:
                    System.Windows.Media.CompositionTarget.Rendering += (s, e) => Tick();
                    return;
#endif
            }

            Windows.UI.Core.CoreWindow? coreWindow = (GameContext as GameContextCoreWindow)?.Control;

            while (true)
            {
                coreWindow?.Dispatcher.ProcessEvents(Windows.UI.Core.CoreProcessEventsOption.ProcessAllIfPresent);
                Tick();
            }
        }

        public void Tick()
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan deltaTime = currentTime - previousTime;
            previousTime = currentTime;

            Update(deltaTime);

            BeginDraw();
            Draw(deltaTime);
            EndDraw();
        }

        protected void Initialize()
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Initialize();
            }
        }

        protected virtual Task LoadContentAsync()
        {
            List<Task> loadingTasks = new List<Task>(GameSystems.Count);

            foreach (GameSystem gameSystem in GameSystems)
            {
                loadingTasks.Add(gameSystem.LoadContentAsync());
            }

            return Task.WhenAll(loadingTasks);
        }

        protected virtual void Update(TimeSpan deltaTime)
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Update(deltaTime);
            }
        }

        protected virtual void BeginDraw()
        {
            if (GraphicsDevice.Presenter is null) return;

            int width = GraphicsDevice.Presenter.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.Presenter.PresentationParameters.BackBufferHeight;

            if (width != GraphicsDevice.Presenter.Viewport.Width || height != GraphicsDevice.Presenter.Viewport.Height)
            {
                GraphicsDevice.Presenter.Resize(width, height);
                GraphicsDevice.Presenter.ResizeViewport(width, height);
            }

            GraphicsDevice.Presenter.BeginDraw(GraphicsDevice.CommandList);

            GraphicsDevice.CommandList.Reset();

            GraphicsDevice.CommandList.SetViewport(GraphicsDevice.Presenter.Viewport);
            GraphicsDevice.CommandList.SetScissorRectangles(GraphicsDevice.Presenter.ScissorRect);
            GraphicsDevice.CommandList.SetRenderTargets(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.BeginDraw();
            }
        }

        protected virtual void Draw(TimeSpan deltaTime)
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.Draw(deltaTime);
            }
        }

        protected virtual void EndDraw()
        {
            foreach (GameSystem gameSystem in GameSystems)
            {
                gameSystem.EndDraw();
            }

            GraphicsDevice.CommandList.Flush(true);

            GraphicsDevice.Presenter?.Present();
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(GraphicsDevice)
                .AddSingleton<ContentManager>()
                .AddSingleton(GameSystems)
                .AddSingleton<SceneSystem>()
                .BuildServiceProvider();
        }
    }
}
