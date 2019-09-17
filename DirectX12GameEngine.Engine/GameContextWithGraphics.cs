using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public class GameContextWithGraphics : GameContextWithFileProvider
    {
        public GraphicsDevice GraphicsDevice { get; } = new GraphicsDevice();

        public PresentationParameters PresentationParameters { get; } = new PresentationParameters();

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton(GraphicsDevice);
        }
    }

    public class GameContextWithGraphics<TControl> : GameContextWithFileProvider<TControl> where TControl : class
    {
        public GameContextWithGraphics(TControl control) : base(control)
        {
        }

        public GraphicsDevice GraphicsDevice { get; } = new GraphicsDevice();

        public PresentationParameters PresentationParameters { get; } = new PresentationParameters();

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton(GraphicsDevice);
        }
    }
}
