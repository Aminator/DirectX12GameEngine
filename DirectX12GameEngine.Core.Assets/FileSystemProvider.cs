using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectX12GameEngine.Core.Assets
{
    public class FileSystemProvider : IFileProvider
    {
        public FileSystemProvider(IStorageFolder rootFolder, IStorageFolder? readWriteRootFolder = null)
        {
            RootFolder = rootFolder;
            ReadWriteRootFolder = readWriteRootFolder ?? rootFolder;
        }

        public string RootPath => RootFolder.Path;

        public IStorageFolder RootFolder { get; }

        public IStorageFolder ReadWriteRootFolder { get; }

        public async Task<bool> ExistsAsync(string path)
        {
            return await ExistsAsync(ReadWriteRootFolder, path)
                || (RootFolder != ReadWriteRootFolder && await ExistsAsync(RootFolder, path));
        }

        public async Task<Stream> OpenStreamAsync(string path, FileMode mode, FileAccess access)
        {
            if (access.HasFlag(FileAccess.Write))
            {
                return await ReadWriteRootFolder.OpenStreamForWriteAsync(path, ToCreationCollisionOption(mode));
            }
            else
            {
                if (mode != FileMode.Open) throw new ArgumentException("File mode has to be FileMode.Open when FileAccess.Read is specified.");

                if (await ExistsAsync(ReadWriteRootFolder, path))
                {
                    return await ReadWriteRootFolder.OpenStreamForReadAsync(path);
                }

                if (RootFolder != ReadWriteRootFolder && await ExistsAsync(RootFolder, path))
                {
                    return await RootFolder.OpenStreamForReadAsync(path);
                }

                throw new FileNotFoundException();
            }
        }

        private async Task<bool> ExistsAsync(IStorageFolder folder, string path)
        {
            return await ((IStorageFolder2)folder).TryGetItemAsync(path) != null;
        }

        private CreationCollisionOption ToCreationCollisionOption(FileMode mode) => mode switch
        {
            FileMode.CreateNew => CreationCollisionOption.FailIfExists,
            FileMode.Create => CreationCollisionOption.ReplaceExisting,
            FileMode.OpenOrCreate => CreationCollisionOption.OpenIfExists,
            _ => throw new NotSupportedException()
        };
    }
}
