using System.Threading.Tasks;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class CodeEditorViewFactory : IEditorViewFactory
    {
        public Task<object?> CreateAsync(StorageFileViewModel item)
        {
            return Task.FromResult<object?>(new CodeEditorViewModel(item));
        }
    }
}
