using DirectX12GameEngine.Editor.Commanding;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionExplorerViewModel : ViewModelBase
    {
        private StorageFolderViewModel? rootFolder;

        public SolutionExplorerViewModel()
        {
            OpenCommand = new RelayCommand<StorageItemViewModel>(Open);
            DeleteCommand = new RelayCommand<StorageItemViewModel>(Delete);
            RefreshCommand = new RelayCommand(Refresh, () => RootFolder != null);

            Messenger.Default.Register<ProjectLoadedMessage>(this, m =>
            {
                RootFolder = m.RootFolder;
                RootFolder.IsExpanded = true;
            });
        }

        public StorageFolderViewModel? RootFolder
        {
            get => rootFolder;
            set => Set(ref rootFolder, value);
        }

        public RelayCommand<StorageItemViewModel> OpenCommand { get; }

        public RelayCommand<StorageItemViewModel> DeleteCommand { get; }

        public RelayCommand RefreshCommand { get; }

        private void Open(StorageItemViewModel item)
        {
            Messenger.Default.Send(new LaunchStorageItemMessage(item));
        }

        private void Delete(StorageItemViewModel item)
        {
            item.Parent?.Children.Remove(item);
        }

        private void Refresh()
        {
            if (RootFolder != null)
            {
                RootFolder = new StorageFolderViewModel(RootFolder.Model)
                {
                    IsExpanded = true
                };
            }
        }
    }
}
