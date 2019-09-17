using DirectX12GameEngine.Core.Assets;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel;
using Windows.Storage;

namespace DirectX12GameEngine.Games
{
    public abstract class GameContext
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
        }
    }

    public abstract class GameContextWithFileProvider : GameContext
    {
        public IFileProvider? FileProvider { get; set; }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            IFileProvider fileProvider = FileProvider ?? new FileSystemProvider(Package.Current.InstalledLocation, ApplicationData.Current.TemporaryFolder);

            services.AddSingleton(fileProvider);
        }
    }

    public abstract class GameContextWithFileProvider<TControl> : GameContextWithFileProvider where TControl : class
    {
        public TControl Control { get; protected set; }

        protected GameContextWithFileProvider(TControl control)
        {
            Control = control;
        }
    }

    public class NullGameContext : GameContext
    {
    }
}
