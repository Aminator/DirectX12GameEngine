using System;
using System.Threading.Tasks;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public interface IEditorViewFactory
    {
        public Task<object?> CreateAsync(IStorageFile item, IServiceProvider services);
    }
}
