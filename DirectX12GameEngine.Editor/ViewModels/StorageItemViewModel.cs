using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class StorageItemViewModel : ViewModelBase<IStorageItem>
    {
        private StorageItemViewModel? parent;
        private bool hasUnrealizedChildren;
        private bool isExpanded;

        public StorageItemViewModel(IStorageItem model) : base(model)
        {
            Children.CollectionChanged += Children_CollectionChanged;

            PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(IsExpanded))
                {
                    if (IsExpanded)
                    {
                        await FillAsync();
                    }
                }
            };
        }

        public ObservableCollection<StorageItemViewModel> Children { get; } = new ObservableCollection<StorageItemViewModel>();

        public StorageItemViewModel? Parent
        {
            get => parent;
            private set => Set(ref parent, value);
        }

        public bool HasUnrealizedChildren
        {
            get => hasUnrealizedChildren;
            set => Set(ref hasUnrealizedChildren, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => Set(ref isExpanded, value);
        }

        public string Name
        {
            get => Model.Name;
        }

        public async Task FillAsync()
        {
            if (Model is IStorageFolder folder && HasUnrealizedChildren)
            {
                var items = await folder.GetItemsAsync();

                if (items.Count == 0) return;

                foreach (IStorageItem childItem in items)
                {
                    StorageItemViewModel newItem = new StorageItemViewModel(childItem);

                    if (newItem.Model is IStorageFolder)
                    {
                        newItem.HasUnrealizedChildren = true;
                    }

                    Children.Add(newItem);
                }

                HasUnrealizedChildren = false;
            }
        }

        public Task OpenAsync()
        {
            Messenger.Default.Send(new LaunchStorageItemMessage(this));

            return Task.CompletedTask;
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
