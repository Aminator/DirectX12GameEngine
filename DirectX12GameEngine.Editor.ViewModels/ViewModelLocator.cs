using System;
using DirectX12GameEngine.Editor.ViewModels.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddSingleton<SolutionLoaderViewModel>();
            serviceCollection.AddSingleton<SolutionExplorerViewModel>();
            serviceCollection.AddSingleton<PropertiesViewModel>();
            serviceCollection.AddSingleton<SdkManagerViewModel>();
            serviceCollection.AddSingleton<EditorViewFactory>();

            Services = serviceCollection.BuildServiceProvider();
        }

        public IServiceProvider Services { get; }
    }
}
