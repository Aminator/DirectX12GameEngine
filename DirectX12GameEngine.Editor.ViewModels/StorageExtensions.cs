using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        public static Task<StorageFolder> CopyAsync(this IStorageFolder source, IStorageFolder destinationFolder, NameCollisionOption option, params string[] folderNamesToIgnore)
        {
            return CopyAsync(source, destinationFolder, option, null, folderNamesToIgnore);
        }

        public static async Task<StorageFolder> CopyAsync(this IStorageFolder source, IStorageFolder destinationFolder, NameCollisionOption option, IProgress<double>? progress, params string[] folderNamesToIgnore)
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

            StorageFolder newDestinationFolder = await CopyAsync(source, destinationFolder);
            progress?.Report(1);

            return newDestinationFolder;

            async Task<StorageFolder> CopyAsync(IStorageFolder source, IStorageFolder destinationFolder)
            {
                StorageFolder newDestinationFolder = await destinationFolder.CreateFolderAsync(source.Name, CreationCollisionOption.OpenIfExists);

                foreach (var item in (await newDestinationFolder.GetItemsAsync()).Where(i => !folderNamesToIgnore.Contains(i.Name)))
                {
                    if (await ((IStorageFolder2)source).TryGetItemAsync(item.Name) is null)
                    {
                        await item.DeleteAsync();
                    }
                }

                foreach (var file in await source.GetFilesAsync())
                {
                    fileCounter++;

                    if (fileCount != 0)
                    {
                        progress?.Report((double)fileCounter / fileCount);
                    }

                    bool needToCopyFile = true;

                    //if (await newDestinationFolder.TryGetItemAsync(file.Name) is StorageFile existingFile)
                    //{
                    //    byte[] existingFileHash = await ComputeHashOfFileAsync(existingFile);
                    //    byte[] newFileHash = await ComputeHashOfFileAsync(file);

                    //    needToCopyFile = !existingFileHash.SequenceEqual(newFileHash);
                    //}

                    if (needToCopyFile)
                    {
                        await file.CopyAsync(newDestinationFolder, file.Name, option);
                    }
                }

                foreach (var folder in (await source.GetFoldersAsync()).Where(f => !folderNamesToIgnore.Contains(f.Name)))
                {
                    await CopyAsync(folder, newDestinationFolder);
                }

                return newDestinationFolder;
            }
        }

        public async static Task<StorageFolder> MoveAsync(this IStorageFolder source, IStorageFolder destinationFolder, NameCollisionOption option)
        {
            StorageFolder newDestinationFolder = await MoveAsync(source, destinationFolder);
            await source.DeleteAsync();

            return newDestinationFolder;

            async Task<StorageFolder> MoveAsync(IStorageFolder source, IStorageFolder destinationFolder)
            {
                StorageFolder newDestinationFolder = await destinationFolder.CreateFolderAsync(source.Name, CreationCollisionOption.OpenIfExists);

                foreach (var file in await source.GetFilesAsync())
                {
                    await file.MoveAsync(newDestinationFolder, file.Name, option);
                }

                foreach (var folder in await source.GetFoldersAsync())
                {
                    await MoveAsync(folder, newDestinationFolder);
                }

                return newDestinationFolder;
            }
        }

        public static async Task<byte[]> ComputeHashOfFileAsync(IStorageFile file)
        {
            using Stream stream = await file.OpenStreamForReadAsync();
            using SHA256 sha256 = SHA256.Create();

            return sha256.ComputeHash(stream);
        }

        public static string GetRelativePath(string relativeTo, string path)
        {
            relativeTo = Path.IsPathRooted(relativeTo) ? relativeTo : "C:/" + relativeTo;
            path = Path.IsPathRooted(path) ? path : "C:/" + path;

            Uri path1 = new Uri(relativeTo + Path.DirectorySeparatorChar);
            Uri path2 = new Uri(path);

            Uri relativeUri = path1.MakeRelativeUri(path2);
            return Uri.UnescapeDataString(relativeUri.OriginalString.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
