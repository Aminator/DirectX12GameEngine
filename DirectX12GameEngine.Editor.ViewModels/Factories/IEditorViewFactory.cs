using System.Threading.Tasks;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public interface IEditorViewFactory
    {
        public Task<object?> CreateAsync(StorageFileViewModel item);
    }
}
