using DirectX12GameEngine.Editor.ViewModels.Factories;
using DirectX12GameEngine.Editor.ViewModels.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class ViewModelLocator
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IFileEditorViewFactory, FileEditorViewFactory>();
            services.AddSingleton<ISolutionLoader, SolutionLoader>();
            services.AddSingleton<IPropertyManager, PropertyManager>();
            services.AddSingleton<ISdkManager, SdkManager>();
            services.AddSingleton<ITabViewManager, TabViewManager>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SolutionLoaderViewModel>();
            services.AddSingleton<PropertyManagerViewModel>();
            services.AddSingleton<SdkManagerViewModel>();
            services.AddSingleton<SolutionExplorerViewModel>();
            services.AddSingleton<TabViewManagerViewModel>();
        }
    }
}
