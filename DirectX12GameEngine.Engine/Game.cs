using System;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Input;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public class Game : GameBase
    {
        public Game(GameContext context) : base(context)
        {
            GraphicsDevice = Services.GetService<GraphicsDevice>();

            if (GraphicsDevice != null)
            {
                GraphicsDevice.Presenter = Services.GetService<GraphicsPresenter>();
            }

            Window = Services.GetService<GameWindow>();

            Input = Services.GetRequiredService<InputManager>();
            Script = Services.GetRequiredService<ScriptSystem>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();

            GameSystems.Add(Input);
            GameSystems.Add(Script);
            GameSystems.Add(SceneSystem);
        }

        public GraphicsDevice? GraphicsDevice { get; }

        public GameWindow? Window { get; set; }

        public InputManager Input { get; }

        public ScriptSystem Script { get; }

        public SceneSystem SceneSystem { get; }

        public override void Dispose()
        {
            GraphicsDevice?.Presenter?.Dispose();
            GraphicsDevice?.Dispose();

            base.Dispose();
        }

        public override void Initialize()
        {
            if (Window != null)
            {
                Window.TickRequested += OnTickRequested;
            }

            base.Initialize();
        }

        public override void BeginRun()
        {
            base.BeginRun();

            Window?.Run();
        }

        public override void BeginDraw()
        {
            if (GraphicsDevice != null)
            {
                GraphicsDevice.CommandList.Reset();

                if (Window != null && GraphicsDevice.Presenter != null)
                {
                    int windowWidth = (int)Window.ClientBounds.Width;
                    int windowHeight = (int)Window.ClientBounds.Height;

                    if (windowWidth != GraphicsDevice.Presenter.BackBuffer.Width || windowHeight != GraphicsDevice.Presenter.BackBuffer.Height)
                    {
                        GraphicsDevice.Presenter.Resize(windowWidth, windowHeight);
                    }
                }

                GraphicsDevice.CommandList.ClearState();

                GraphicsDevice.Presenter?.BeginDraw(GraphicsDevice.CommandList);
            }

            base.BeginDraw();
        }

        public override void EndDraw()
        {
            base.EndDraw();

            GraphicsDevice?.CommandList.Flush();
            GraphicsDevice?.Presenter?.Present();
        }

        public override void EndRun()
        {
            base.EndRun();

            if (Window != null)
            {
                Window.Exit();
                Window.TickRequested -= OnTickRequested;
            }
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<InputManager>();
            services.AddSingleton<SceneSystem>();
            services.AddSingleton<ScriptSystem>();
        }

        private void OnTickRequested(object? sender, EventArgs e)
        {
            Tick();
        }
    }
}
