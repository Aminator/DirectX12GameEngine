using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class StorageFolderViewModel : StorageItemViewModel
    {
        private bool hasUnrealizedChildren = true;
        private bool isExpanded;

        public StorageFolderViewModel(IStorageFolder model) : base(model)
        {
            Children = new ObservableStorageItemCollection(this);
        }

        public new IStorageFolder Model => (IStorageFolder)base.Model;

        public ObservableStorageItemCollection Children { get; }

        public bool HasUnrealizedChildren
        {
            get => hasUnrealizedChildren;
            set => Set(ref hasUnrealizedChildren, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (Set(ref isExpanded, value))
                {
                    if (isExpanded)
                    {
                        if (HasUnrealizedChildren)
                        {
                            _ = FillAsync();
                        }
                    }
                }
            }
        }

        public async Task FillAsync()
        {
            Children.Clear();

            var items = await Model.GetItemsAsync();

            foreach (IStorageItem childItem in items)
            {
                if (childItem is IStorageFile file)
                {
                    Children.Add(new StorageFileViewModel(file));
                }
                else if (childItem is IStorageFolder folder)
                {
                    Children.Add(new StorageFolderViewModel(folder));
                }
            }

            HasUnrealizedChildren = false;
        }
    }

    public class ObservableStorageItemCollection : ObservableCollection<StorageItemViewModel>
    {
        private readonly StorageFolderViewModel storageFolder;

        public ObservableStorageItemCollection(StorageFolderViewModel storageFolder)
        {
            this.storageFolder = storageFolder;
        }

        protected override async void InsertItem(int index, StorageItemViewModel item)
        {
            StorageItemViewModel? existingItem = this.FirstOrDefault(i => i.Name == item.Name);

            base.InsertItem(index, item);

            if (item.Parent != null)
            {
                throw new InvalidOperationException("This item already has parent.");
            }

            item.Parent = storageFolder;

            if (existingItem != null)
            {
                if (existingItem is StorageFileViewModel)
                {
                    Remove(existingItem);
                }
                else if (existingItem is StorageFolderViewModel)
                {
                    Remove(item);
                }
            }

            if (item.Model is IStorageItem2 item2)
            {
                StorageFolder parent = await item2.GetParentAsync();

                if (!parent.IsEqual(storageFolder.Model))
                {
                    if (item2 is IStorageFile file)
                    {
                        await file.MoveAsync(storageFolder.Model, file.Name, NameCollisionOption.ReplaceExisting);
                    }
                    else if (item2 is IStorageFolder folder)
                    {
                        StorageFolder newFolder = await folder.MoveAsync(storageFolder.Model, NameCollisionOption.ReplaceExisting);
                        StorageFolderViewModel? existingFolder = (StorageFolderViewModel?)this.FirstOrDefault(i => ((IStorageItem2)i.Model).IsEqual(newFolder));

                        if (existingFolder != null)
                        {
                            await existingFolder.FillAsync();
                        }
                        else
                        {
                            Insert(index, new StorageFolderViewModel(newFolder));
                        }
                    }
                }
            }
        }

        protected override void RemoveItem(int index)
        {
            StorageItemViewModel item = base[index];

            base.RemoveItem(index);

            if (item.Parent != storageFolder)
            {
                throw new InvalidOperationException("This item is not a child of the expected parent.");
            }

            item.Parent = null;
        }
    }
}
