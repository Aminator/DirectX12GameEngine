using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class StorageFolderViewModel : StorageItemViewModel
    {
        private bool hasUnrealizedChildren = true;
        private bool isExpanded;

        public StorageFolderViewModel(IStorageFolder model) : base(model)
        {
            Model = model;

            Children.CollectionChanged += Children_CollectionChanged;
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
                        Fill();
                    }
                }
            }
        }

        private async void Fill()
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

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
