using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Editor.Views;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public class CodeEditorViewFactory : IAssetViewFactory
    {
        public Task<object?> CreateAsync(StorageFileViewModel item)
        {
            return Task.FromResult<object?>(new CodeEditorView { DataContext = new CodeEditorViewModel(item) });
        }
    }
}
