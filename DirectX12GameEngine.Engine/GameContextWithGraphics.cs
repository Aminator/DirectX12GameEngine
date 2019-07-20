using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public class GameContextWithGraphics<TControl> : GameContext<TControl> where TControl : class
    {
        public GameContextWithGraphics(TControl control) : base(control)
        {
        }

        public PresentationParameters PresentationParameters { get; } = new PresentationParameters();

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GraphicsDevice>();
            services.AddSingleton(PresentationParameters);
        }
    }
}
