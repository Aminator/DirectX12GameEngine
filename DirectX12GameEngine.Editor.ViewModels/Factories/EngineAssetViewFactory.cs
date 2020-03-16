using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class EngineAssetViewFactory : IEditorViewFactory
    {
        private readonly Dictionary<Type, IEditorViewFactory> factories = new Dictionary<Type, IEditorViewFactory>();

        public void Add(Type type, IEditorViewFactory factory)
        {
            factories.Add(type, factory);
        }

        public async Task<object?> CreateAsync(IStorageFile item, IServiceProvider services)
        {
            using Stream stream = await item.OpenStreamForReadAsync();

            Type? type = ContentManager.GetRootObjectType(stream);

            if (type != null && factories.TryGetValue(type, out IEditorViewFactory factory))
            {
                return await factory.CreateAsync(item, services);
            }

            return null;
        }
    }
}
