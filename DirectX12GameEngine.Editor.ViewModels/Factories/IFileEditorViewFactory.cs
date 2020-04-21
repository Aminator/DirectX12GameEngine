using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public interface IFileEditorViewFactory : IEditorViewFactory
    {
        public IServiceProvider Services { get; }

        public void Add(string fileExtension, IEditorViewFactory factory);

        Task<object?> CreateAsync(IStorageFile item);
    }
}
