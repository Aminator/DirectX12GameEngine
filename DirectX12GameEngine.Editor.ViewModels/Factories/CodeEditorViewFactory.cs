using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public class CodeEditorViewFactory : IEditorViewFactory
    {
        public Task<object?> CreateAsync(StorageFileViewModel item)
        {
            return Task.FromResult<object?>(new CodeEditorViewModel(item));
        }
    }
}
