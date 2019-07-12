using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Xaml;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public interface IAssetViewFactory
    {
        public Task<UIElement?> CreateAsync(StorageItemViewModel item);
    }
}
