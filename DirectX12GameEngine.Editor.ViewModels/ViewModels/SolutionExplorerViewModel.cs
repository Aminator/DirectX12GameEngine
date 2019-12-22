using System.Threading.Tasks;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using DirectX12GameEngine.Mvvm.Messaging;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionExplorerViewModel : ViewModelBase
    {
        private StorageFolderViewModel? rootFolder;

        public SolutionExplorerViewModel()
        {
            OpenCommand = new RelayCommand<StorageItemViewModel>(Open);
            ViewCodeCommand = new RelayCommand<StorageFileViewModel>(ViewCode);
            DeleteCommand = new RelayCommand<StorageItemViewModel>(Delete);
            ShowPropertiesCommand = new RelayCommand<StorageItemViewModel>(ShowProperties);

            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => RootFolder != null);

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

        public RelayCommand<StorageFileViewModel> ViewCodeCommand { get; }

        public RelayCommand<StorageItemViewModel> DeleteCommand { get; }

        public RelayCommand<StorageItemViewModel> ShowPropertiesCommand { get; }

        public RelayCommand RefreshCommand { get; }

        public void Open(StorageItemViewModel item)
        {
            Messenger.Default.Send(new LaunchStorageItemMessage(item));
        }

        public void ViewCode(StorageFileViewModel file)
        {
            Messenger.Default.Send(new ViewCodeMessage(file));
        }

        public void Delete(StorageItemViewModel item)
        {
            item.Parent?.Children.Remove(item);
        }

        public void ShowProperties(StorageItemViewModel item)
        {
            Messenger.Default.Send(new ShowPropertiesMessage(item.Model));
        }

        public async Task RefreshAsync()
        {
            if (RootFolder != null)
            {
                RootFolder = new StorageFolderViewModel(RootFolder.Model);
                await RootFolder.FillAsync();
                RootFolder.IsExpanded = true;
            }
        }
    }
}
