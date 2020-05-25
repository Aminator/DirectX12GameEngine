using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels.Properties;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Factories
{
    public class SceneEditorViewFactory : IEditorViewFactory
    {
        private static readonly HashSet<string> dynamicallyLoadedAssemblyPaths = new HashSet<string>();

        public async Task<object?> CreateAsync(IStorageFile item, IServiceProvider services)
        {
            StorageFolder? parentFolder = await (item as IStorageItem2)?.GetParentAsync();

            if (parentFolder is null) return null;

            StorageFile? projectFile = await parentFolder.GetFileWithExtensionInHierarchyAsync("proj");

            if (projectFile is null) throw new FileNotFoundException("No project file could be found.");

            StorageFolder projectFolder = await projectFile.GetParentAsync();

            await LoadAssemblyAsync(projectFolder, Path.GetFileNameWithoutExtension(projectFile.Name));

            string scenePath = StorageExtensions.GetRelativePath(projectFolder.Path, item.Path);
            scenePath = Path.Combine(Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath));

            return new SceneEditorViewModel(projectFolder, scenePath, services.GetRequiredService<IPropertyManager>());
        }

        private static async Task LoadAssemblyAsync(IStorageFolder folder, string assemblyName)
        {
            try
            {
                StorageFile assemblyFile = await folder.GetFileAsync(Path.Combine(@"bin\Debug\netstandard2.0", assemblyName + ".dll"));

                string assemblyCopyPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, assemblyFile.Name);

                if (!dynamicallyLoadedAssemblyPaths.Contains(assemblyCopyPath))
                {
                    dynamicallyLoadedAssemblyPaths.Add(assemblyCopyPath);

                    StorageFile assemblyFileCopy = await assemblyFile.CopyAsync(ApplicationData.Current.TemporaryFolder, assemblyFile.Name, NameCollisionOption.ReplaceExisting);

                    Assembly.LoadFrom(assemblyFileCopy.Path);
                }
            }
            catch
            {
            }
        }
    }
}
