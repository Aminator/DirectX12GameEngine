using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
            Model = model;

            Children.CollectionChanged += OnChildrenCollectionChanged;
        }

        public new IStorageFolder Model { get; }

        public ObservableCollection<StorageItemViewModel> Children { get; } = new ObservableCollection<StorageItemViewModel>();

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
                        _ = FillAsync();
                    }
                }
            }
        }

        public async Task<IEnumerable<StorageFileViewModel>> GetFilesAsync()
        {
            await FillAsync();
            return Children.OfType<StorageFileViewModel>();
        }

        public async Task<IEnumerable<StorageFolderViewModel>> GetFoldersAsync()
        {
            await FillAsync();
            return Children.OfType<StorageFolderViewModel>();
        }

        public async Task<IEnumerable<StorageItemViewModel>> GetItemsAsync()
        {
            await FillAsync();
            return Children;
        }

        public async Task FillAsync()
        {
            if (HasUnrealizedChildren)
            {
                var items = await Model.GetItemsAsync();

                if (items.Count == 0) return;

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

        private void AddInternal(StorageItemViewModel item)
        {
            if (item.Parent != null)
            {
                throw new InvalidOperationException("This item already has parent.");
            }

            item.Parent = this;
        }

        private void RemoveInternal(StorageItemViewModel item)
        {
            if (item.Parent != this)
            {
                throw new InvalidOperationException("This item is not a child of the expected parent.");
            }

            item.Parent = null;
        }

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (StorageItemViewModel item in e.NewItems)
                    {
                        AddInternal(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (StorageItemViewModel item in e.OldItems)
                    {
                        RemoveInternal(item);
                    }
                    break;
            }
        }
    }
}
