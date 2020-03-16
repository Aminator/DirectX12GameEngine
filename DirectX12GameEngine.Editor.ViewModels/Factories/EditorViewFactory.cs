using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class EditorViewFactory : IEditorViewFactory
    {
        private readonly Dictionary<string, IEditorViewFactory> factories = new Dictionary<string, IEditorViewFactory>();

        public EditorViewFactory(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public void Add(string fileExtension, IEditorViewFactory factory)
        {
            factories.Add(fileExtension, factory);
        }

        public Task<object?> CreateAsync(IStorageFile item)
        {
            return CreateAsync(item, Services);
        }

        public async Task<object?> CreateAsync(IStorageFile item, IServiceProvider services)
        {
            if (factories.TryGetValue(item.FileType, out IEditorViewFactory factory))
            {
                return await factory.CreateAsync(item, services);
            }

            return null;
        }
    }
}
