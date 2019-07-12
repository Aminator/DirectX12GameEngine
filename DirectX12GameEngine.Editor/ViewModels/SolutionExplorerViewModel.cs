using System.Threading.Tasks;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionExplorerViewModel : ViewModelBase
    {
        private StorageItemViewModel? rootFolder;

        public SolutionExplorerViewModel()
        {
            Messenger.Default.Register<ProjectLoadedMessage>(this, async m => await SetRootFolderAsync(m.RootFolder));
        }

        public StorageItemViewModel? RootFolder
        {
            get => rootFolder;
            private set => Set(ref rootFolder, value);
        }

        public async Task SetRootFolderAsync(StorageItemViewModel folder)
        {
            RootFolder = folder;
            RootFolder.HasUnrealizedChildren = true;
            await RootFolder.FillAsync();
        }
    }
}
