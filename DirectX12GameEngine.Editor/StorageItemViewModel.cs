using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace DirectX12GameEngine.Editor
{
    public class StorageItemViewModel : ViewModelBase<IStorageItem>
    {
        private bool hasUnrealizedChildren;

        public StorageItemViewModel(IStorageItem model) : base(model)
        {
        }

        public IStorageItem StorageItem => Model;

        public ObservableCollection<StorageItemViewModel> Children { get; } = new ObservableCollection<StorageItemViewModel>();

        public bool HasUnrealizedChildren
        {
            get => hasUnrealizedChildren;
            set => Set(ref hasUnrealizedChildren, value);
        }

        public string Name
        {
            get => StorageItem.Name;
        }

        public void Collapse()
        {
            //Children.Clear();
            //HasUnrealizedChildren = true;
        }

        public async Task ExpandAsync()
        {
            await FillAsync();
        }

        public async Task FillAsync()
        {
            if (StorageItem is IStorageFolder folder && HasUnrealizedChildren)
            {
                var items = await folder.GetItemsAsync();

                if (items.Count == 0) return;

                foreach (IStorageItem childItem in items)
                {
                    StorageItemViewModel newItem = new StorageItemViewModel(childItem);

                    if (newItem.StorageItem is IStorageFolder)
                    {
                        newItem.HasUnrealizedChildren = true;
                    }

                    Children.Add(newItem);
                }

                HasUnrealizedChildren = false;
            }
        }

        public async Task<bool> OpenAsync()
        {
            if (StorageItem is IStorageFile file)
            {
                return await Launcher.LaunchFileAsync(file);
            }
            else if (StorageItem is IStorageFolder folder)
            {
                return await Launcher.LaunchFolderAsync(folder);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
