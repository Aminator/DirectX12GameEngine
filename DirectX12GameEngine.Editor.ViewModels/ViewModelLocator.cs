using System;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<MainViewModel>();

            Services = serviceCollection.BuildServiceProvider();
        }

        public IServiceProvider Services { get; }
    }
}
