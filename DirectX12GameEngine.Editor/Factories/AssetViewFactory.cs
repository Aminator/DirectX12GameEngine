using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public class AssetViewFactory : IAssetViewFactory
    {
        private static AssetViewFactory? defaultInstance;

        private readonly Dictionary<string, IAssetViewFactory> factories = new Dictionary<string, IAssetViewFactory>();

        public static AssetViewFactory Default
        {
            get => defaultInstance ?? (defaultInstance = new AssetViewFactory());
            set => defaultInstance = value;
        }

        public void Add(string fileExtension, IAssetViewFactory factory)
        {
            factories.Add(fileExtension, factory);
        }

        public async Task<object?> CreateAsync(StorageFileViewModel item)
        {
            if (factories.TryGetValue(item.Model.FileType, out IAssetViewFactory factory))
            {
                return await factory.CreateAsync(item);
            }

            return null;
        }
    }
}
