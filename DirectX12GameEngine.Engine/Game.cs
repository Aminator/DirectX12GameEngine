using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Input;
using DirectX12GameEngine.Rendering.Materials;
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

            Input = Services.GetRequiredService<InputManager>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();
            Script = Services.GetRequiredService<ScriptSystem>();

            GameSystems.Add(Input);
            GameSystems.Add(SceneSystem);
            GameSystems.Add(Script);
        }

        public GraphicsDevice? GraphicsDevice { get; set; }

        public InputManager Input { get; }

        public SceneSystem SceneSystem { get; }

        public ScriptSystem Script { get; }

        public override void Dispose()
        {
            base.Dispose();

            if (GraphicsDevice != null)
            {
                if (GraphicsDevice.Presenter != null)
                {
                    GraphicsDevice.Presenter.Dispose();
                    GraphicsDevice.Presenter = null;
                }

                GraphicsDevice.Dispose();
                GraphicsDevice = null;
            }
        }

        protected override void BeginDraw()
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

        protected override void EndDraw()
        {
            base.EndDraw();

            GraphicsDevice?.CommandList.Flush(true);
            GraphicsDevice?.Presenter?.Present();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<InputManager>();
            services.AddSingleton<SceneSystem>();
            services.AddSingleton<ScriptSystem>();
        }
    }
}
