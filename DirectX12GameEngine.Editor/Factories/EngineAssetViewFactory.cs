using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Editor.ViewModels;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public class EngineAssetViewFactory : IAssetViewFactory
    {
        private readonly Dictionary<Type, IAssetViewFactory> factories = new Dictionary<Type, IAssetViewFactory>();

        public void Add(Type type, IAssetViewFactory factory)
        {
            factories.Add(type, factory);
        }

        public async Task<object?> CreateAsync(StorageFileViewModel item)
        {
            using Stream stream = await item.Model.OpenStreamForReadAsync();

            Type? type = ContentManager.GetRootObjectType(stream);

            if (type != null && factories.TryGetValue(type, out IAssetViewFactory factory))
            {
                return await factory.CreateAsync(item);
            }

            return null;
        }
    }
}
