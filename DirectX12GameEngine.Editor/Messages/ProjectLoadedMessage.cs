using DirectX12GameEngine.Editor.ViewModels;

namespace DirectX12GameEngine.Editor.Messages
{
    public class ProjectLoadedMessage
    {
        public ProjectLoadedMessage(StorageItemViewModel rootFolder)
        {
            RootFolder = rootFolder;
        }

        public StorageItemViewModel RootFolder { get; }
    }
}
