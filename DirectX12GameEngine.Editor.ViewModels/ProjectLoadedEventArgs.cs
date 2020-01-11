using System;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class ProjectLoadedEventArgs : EventArgs
    {
        public ProjectLoadedEventArgs(StorageFolderViewModel rootFolder)
        {
            RootFolder = rootFolder;
        }

        public StorageFolderViewModel RootFolder { get; }
    }
}
