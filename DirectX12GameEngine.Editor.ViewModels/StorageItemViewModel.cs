using DirectX12GameEngine.Mvvm;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public abstract class StorageItemViewModel : ViewModelBase<IStorageItem>
    {
        private StorageFolderViewModel? parent;

        public StorageItemViewModel(IStorageItem model) : base(model)
        {
        }

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
    }
}
