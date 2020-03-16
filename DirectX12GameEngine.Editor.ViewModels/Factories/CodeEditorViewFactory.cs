using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class CodeEditorViewFactory : IEditorViewFactory
    {
        public Task<object?> CreateAsync(IStorageFile item, IServiceProvider services)
        {
            return Task.FromResult<object?>(new CodeEditorViewModel(item, services.GetRequiredService<SolutionLoaderViewModel>()));
        }
    }
}
