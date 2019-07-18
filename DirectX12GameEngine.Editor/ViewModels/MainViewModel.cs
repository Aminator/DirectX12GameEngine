#nullable enable

using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private StorageItemViewModel? rootFolder;

        public MainViewModel()
        {
            RegisterMessages();
        }

        public EditorViewsViewModel EditorViews { get; } = new EditorViewsViewModel();

        public ProjectLoaderViewModel ProjectLoader { get; } = new ProjectLoaderViewModel();

        public StorageItemViewModel? RootFolder
        {
            get => rootFolder;
            set => Set(ref rootFolder, value);
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<ProjectLoadedMessage>(this, async m =>
            {
                RootFolder = m.RootFolder;
                RootFolder.HasUnrealizedChildren = true;
                await RootFolder.FillAsync();
            });
        }
    }
}
