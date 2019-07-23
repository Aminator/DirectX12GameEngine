using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public interface IAssetViewFactory
    {
        public Task<object?> CreateAsync(StorageItemViewModel item);
    }
}
