using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public static class StorageExtensions
    {
        public static async Task<StorageFile?> GetFileWithExtensionInHierarchyAsync(this IStorageFolder folder, string searchFilter)
        {
            StorageFile? file = (await folder.GetFilesAsync()).FirstOrDefault(s => Path.GetExtension(s.Name).Contains(searchFilter));

            if (file is null)
            {
                StorageFolder? parentFolder = await (folder as IStorageItem2)?.GetParentAsync();

                if (parentFolder != null)
                {
                    return await GetFileWithExtensionInHierarchyAsync(parentFolder, searchFilter);
                }
            }

            return file;
        }

        public static Task<StorageFolder> CopyAsync(this IStorageFolder source, IStorageFolder destinationContainer, NameCollisionOption option, params string[] folderNamesToIgnore)
        {
            return CopyAsync(source, destinationContainer, option, null, folderNamesToIgnore);
        }

        public static async Task<StorageFolder> CopyAsync(this IStorageFolder source, IStorageFolder destinationContainer, NameCollisionOption option, IProgress<double>? progress, params string[] folderNamesToIgnore)
        {
            int fileCount = 0;
            int fileCounter = 0;

            if (progress != null && source is StorageFolder storageFolder)
            {
                if (storageFolder.IsCommonFileQuerySupported(CommonFileQuery.OrderByName))
                {
                    fileCount = (int)await storageFolder.CreateFileQuery(CommonFileQuery.OrderByName).GetItemCountAsync();
                }
            }

            StorageFolder destinationFolder = await CopyAsync(source, destinationContainer);
            progress?.Report(1);

            return destinationFolder;

            async Task<StorageFolder> CopyAsync(IStorageFolder source, IStorageFolder destinationContainer)
            {
                StorageFolder destinationFolder = await destinationContainer.CreateFolderAsync(source.Name, (CreationCollisionOption)option);

                foreach (var file in await source.GetFilesAsync())
                {
                    fileCounter++;

                    if (fileCount != 0)
                    {
                        progress?.Report((double)fileCounter / fileCount);
                    }

                    await file.CopyAsync(destinationFolder, file.Name, option);
                }

                foreach (var folder in (await source.GetFoldersAsync()).Where(f => !folderNamesToIgnore.Contains(f.Name)))
                {
                    await CopyAsync(folder, destinationFolder);
                }

                return destinationFolder;
            }
        }

        public static string GetRelativePath(string relativeTo, string path)
        {
            relativeTo = Path.IsPathRooted(relativeTo) ? relativeTo : "C:/" + relativeTo;
            path = Path.IsPathRooted(path) ? path : "C:/" + path;

            Uri path1 = new Uri(relativeTo + Path.DirectorySeparatorChar);
            Uri path2 = new Uri(path);

            Uri relativeUri = path1.MakeRelativeUri(path2);
            return relativeUri.OriginalString.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
