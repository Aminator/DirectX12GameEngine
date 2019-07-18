using System;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class ViewModelLocator
    {
        private readonly IServiceProvider services;

        public ViewModelLocator()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<MainViewModel>();

            services = serviceCollection.BuildServiceProvider();
        }

        public MainViewModel Main => services.GetRequiredService<MainViewModel>();
    }
}
