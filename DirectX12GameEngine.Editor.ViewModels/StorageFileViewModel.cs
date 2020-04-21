using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class StorageFileViewModel : StorageItemViewModel
    {
        public StorageFileViewModel(IStorageFile model) : base(model)
        {
        }

        public new IStorageFile Model => (IStorageFile)base.Model;

        public bool IsProjectFile => Name.Contains("proj");
    }
}
