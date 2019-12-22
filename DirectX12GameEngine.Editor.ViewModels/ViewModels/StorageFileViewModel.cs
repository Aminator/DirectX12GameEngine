using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class StorageFileViewModel : StorageItemViewModel
    {
        public StorageFileViewModel(IStorageFile model) : base(model)
        {
            Model = model;
        }

        public new IStorageFile Model { get; }
    }
}
