using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Editor.Views;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.Factories
{
    public class SceneViewFactory : IAssetViewFactory
    {
        public async Task<object?> CreateAsync(StorageFileViewModel item)
        {
            if (item.Parent is null) return null;

            string path = Path.GetFileNameWithoutExtension(item.Name);

            StorageFolderViewModel rootFolder = item.Parent;
            StorageFileViewModel? projectFile = (await rootFolder.GetFilesAsync()).FirstOrDefault(s => Path.GetExtension(s.Name).Contains("proj"));

            while (projectFile is null && rootFolder.Parent != null)
            {
                path = Path.Combine(rootFolder.Name, path);

                rootFolder = rootFolder.Parent;
                projectFile = (await rootFolder.GetFilesAsync()).FirstOrDefault(s => Path.GetExtension(s.Name).Contains("proj"));
            }

            if (projectFile != null)
            {
                await LoadAssemblyAsync(rootFolder.Model, Path.GetFileNameWithoutExtension(projectFile.Name));
            }

            SceneView sceneView = new SceneView(rootFolder);
            Task sceneTask = sceneView.ViewModel.LoadAsync(path);

            return sceneView;
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
