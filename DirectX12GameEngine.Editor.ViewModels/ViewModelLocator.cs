using System;
using DirectX12GameEngine.Editor.ViewModels.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class ViewModelLocator
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SolutionLoaderViewModel>();
            services.AddSingleton<SolutionExplorerViewModel>();
            services.AddSingleton<PropertyManagerViewModel>();
            services.AddSingleton<SdkManagerViewModel>();
            services.AddSingleton<EditorViewFactory>();
            services.AddSingleton<TabViewManagerViewModel>();
        }

        public IServiceProvider? Services { get; set; }
    }
}
