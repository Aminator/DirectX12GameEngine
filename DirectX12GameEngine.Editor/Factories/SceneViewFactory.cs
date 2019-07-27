using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Editor.Views;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public class SceneViewFactory : IAssetViewFactory
    {
        public Task<object?> CreateAsync(StorageFileViewModel item)
        {
            if (item.Parent is null) return Task.FromResult<object?>(null);

            string path = Path.GetFileNameWithoutExtension(item.Name);
            StorageFolderViewModel rootFolder = item.Parent;

            while (rootFolder.Parent != null)
            {
                path = Path.Combine(rootFolder.Name, path);
                rootFolder = rootFolder.Parent;
            }

            SceneView sceneView = new SceneView(rootFolder);
            Task sceneTask = sceneView.ViewModel.LoadAsync(path);

            return Task.FromResult<object?>(sceneView);
        }
    }
}
