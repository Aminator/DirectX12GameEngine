using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class SceneEditorViewFactory : IEditorViewFactory
    {
        public async Task<object?> CreateAsync(StorageFileViewModel item)
        {
            if (item.Parent is null) return null;

            string scenePath = Path.GetFileNameWithoutExtension(item.Name);

            StorageFolderViewModel rootFolder = item.Parent;
            StorageFileViewModel? projectFile = (await rootFolder.GetFilesAsync()).FirstOrDefault(s => Path.GetExtension(s.Name).Contains("proj"));

            while (projectFile is null && rootFolder.Parent != null)
            {
                scenePath = Path.Combine(rootFolder.Name, scenePath);

                rootFolder = rootFolder.Parent;
                projectFile = (await rootFolder.GetFilesAsync()).FirstOrDefault(s => Path.GetExtension(s.Name).Contains("proj"));
            }

            if (projectFile != null)
            {
                await LoadAssemblyAsync(rootFolder.Model, Path.GetFileNameWithoutExtension(projectFile.Name));
            }

            return new SceneEditorViewModel(rootFolder, scenePath);
        }

        private async Task LoadAssemblyAsync(IStorageFolder folder, string assemblyName)
        {
            try
            {
                StorageFile assemblyFile = await folder.GetFileAsync(Path.Combine(@"bin\Debug\netstandard2.0", assemblyName + ".dll"));
                StorageFile assemblyFileCopy = await assemblyFile.CopyAsync(ApplicationData.Current.TemporaryFolder, assemblyFile.Name, NameCollisionOption.ReplaceExisting);

                Assembly.LoadFrom(assemblyFileCopy.Path);
            }
            catch
            {
            }
        }
    }
}
