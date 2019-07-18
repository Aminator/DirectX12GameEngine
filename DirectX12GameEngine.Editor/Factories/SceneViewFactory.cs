using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Editor.Views;
using Windows.Storage;
using Windows.UI.Xaml;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public class SceneViewFactory : IAssetViewFactory
    {
        public async Task<UIElement?> CreateAsync(StorageItemViewModel item)
        {
            if (item.Parent is null) return null;

            string path = Path.GetFileNameWithoutExtension(item.Name);
            StorageItemViewModel rootItem = item.Parent;

            while (rootItem.Parent != null)
            {
                path = Path.Combine(rootItem.Name, path);
                rootItem = rootItem.Parent;
            }

            if (rootItem.Model is StorageFolder rootFolder)
            {
                SceneView sceneView = new SceneView(rootFolder);
                Task sceneTask = sceneView.ViewModel.LoadAsync(path);

                return sceneView;
            }

            return null;
        }
    }
}
