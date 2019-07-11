using System;
using System.Threading.Tasks;
using Windows.Storage;

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
            await FillStorageItemAsync(folder);
        }

        public static void Collapse(StorageItemViewModel item)
        {
            //item.Children.Clear();
            //item.HasUnrealizedChildren = true;
        }

        public static async Task ExpandAsync(StorageItemViewModel item)
        {
            if (item.HasUnrealizedChildren)
            {
                await FillStorageItemAsync(item);
            }
        }

        private static async Task FillStorageItemAsync(StorageItemViewModel item)
        {
            if (item.StorageItem is StorageFolder folder && item.HasUnrealizedChildren)
            {
                var items = await folder.GetItemsAsync();

                if (items.Count == 0) return;

                foreach (IStorageItem childItem in items)
                {
                    StorageItemViewModel newItem = new StorageItemViewModel(childItem);

                    if (newItem.StorageItem is StorageFolder)
                    {
                        newItem.HasUnrealizedChildren = true;
                    }

                    item.Children.Add(newItem);
                }

                item.HasUnrealizedChildren = false;
            }
        }
    }
}
