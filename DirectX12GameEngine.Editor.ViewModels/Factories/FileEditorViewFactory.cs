using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Engine;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class FileEditorViewFactory : IFileEditorViewFactory
    {
        private readonly Dictionary<string, IEditorViewFactory> factories = new Dictionary<string, IEditorViewFactory>();

        public FileEditorViewFactory(IServiceProvider services)
        {
            Services = services;

            EngineAssetViewFactory engineAssetViewFactory = new EngineAssetViewFactory();
            engineAssetViewFactory.Add(typeof(Entity), new SceneEditorViewFactory());

            CodeEditorViewFactory codeEditorViewFactory = new CodeEditorViewFactory();

            Add(".xaml", engineAssetViewFactory);
            Add(".cs", codeEditorViewFactory);
            Add(".vb", codeEditorViewFactory);
            Add(".csproj", codeEditorViewFactory);
            Add(".vbproj", codeEditorViewFactory);
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
