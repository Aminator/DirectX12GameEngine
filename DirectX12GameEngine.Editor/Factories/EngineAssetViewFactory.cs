using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.Storage;
using Windows.UI.Xaml;

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

        public async Task<object?> CreateAsync(StorageItemViewModel item)
        {
            if (item.Model is IStorageFile file)
            {
                XElement root;

                using (Stream stream = await file.OpenStreamForReadAsync())
                {
                    root = await XElement.LoadAsync(stream, LoadOptions.None, default);
                }

                Type type = ContentManager.GetTypeFromXmlName(root.Name.NamespaceName, root.Name.LocalName);

                if (factories.TryGetValue(type, out IAssetViewFactory factory))
                {
                    return await factory.CreateAsync(item);
                }
            }

            return null;
        }
    }
}
