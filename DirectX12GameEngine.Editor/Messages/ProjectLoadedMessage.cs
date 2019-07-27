using DirectX12GameEngine.Editor.ViewModels;

namespace DirectX12GameEngine.Editor.Messages
{
    public class ProjectLoadedMessage
    {
        public ProjectLoadedMessage(StorageFolderViewModel rootFolder)
        {
            RootFolder = rootFolder;
        }

        public StorageFolderViewModel RootFolder { get; }
    }
}
