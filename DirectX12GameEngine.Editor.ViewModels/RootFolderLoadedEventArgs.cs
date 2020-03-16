using System;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class RootFolderLoadedEventArgs : EventArgs
    {
        public RootFolderLoadedEventArgs(IStorageFolder rootFolder)
        {
            RootFolder = rootFolder;
        }

        public IStorageFolder RootFolder { get; }
    }
}
