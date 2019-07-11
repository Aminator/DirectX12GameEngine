using System;
using System.Threading.Tasks;

#nullable enable

namespace DirectX12GameEngine.Editor
{
    public class SolutionExplorer : ViewModelBase
    {
        private StorageItemViewModel? rootFolder;

        public StorageItemViewModel? RootFolder
        {
            get => rootFolder;
            private set => Set(ref rootFolder, value);
        }

        public async Task SetRootFolderAsync(StorageItemViewModel folder)
        {
            RootFolder = folder;
            RootFolder.HasUnrealizedChildren = true;
            await RootFolder.FillAsync();
        }
    }
}
