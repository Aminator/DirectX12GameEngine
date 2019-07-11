using System.Collections.ObjectModel;
using Windows.Storage;

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
            get => Model.Name;
        }
    }
}
