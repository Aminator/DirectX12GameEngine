using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public abstract class GameContext
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
        }
    }

    public abstract class GameContext<TControl> : GameContext where TControl : class
    {
        public TControl Control { get; private protected set; }

        protected GameContext(TControl control)
        {
            Control = control;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton(Control);
        }
    }
}
