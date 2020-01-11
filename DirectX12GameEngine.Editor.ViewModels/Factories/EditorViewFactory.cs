using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class EditorViewFactory : IEditorViewFactory
    {
        private static EditorViewFactory? defaultInstance;

        private readonly Dictionary<string, IEditorViewFactory> factories = new Dictionary<string, IEditorViewFactory>();

        public static EditorViewFactory Default
        {
            get => defaultInstance ?? (defaultInstance = new EditorViewFactory());
            set => defaultInstance = value;
        }

        public void Add(string fileExtension, IEditorViewFactory factory)
        {
            factories.Add(fileExtension, factory);
        }

        public async Task<object?> CreateAsync(StorageFileViewModel item)
        {
            if (factories.TryGetValue(item.Model.FileType, out IEditorViewFactory factory))
            {
                return await factory.CreateAsync(item);
            }

            return null;
        }
    }
}
