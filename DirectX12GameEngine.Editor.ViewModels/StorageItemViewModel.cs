using Microsoft.Toolkit.Mvvm.ObjectModel;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public abstract class StorageItemViewModel : ObservableObject
    {
        private StorageFolderViewModel? parent;

        protected StorageItemViewModel(IStorageItem model)
        {
            Model = model;
        }

        public IStorageItem Model { get; }

        public StorageFolderViewModel? Parent
        {
            get => parent;
            set => Set(ref parent, value);
        }

        public string Name
        {
            get => Model.Name;
        }

        public string Path
        {
            get => Model.Path;
        }

        public void OnNameChanged()
        {
            OnPropertyChanged(nameof(Name));
        }
    }
}
