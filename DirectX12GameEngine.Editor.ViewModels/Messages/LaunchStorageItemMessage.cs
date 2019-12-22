using DirectX12GameEngine.Editor.ViewModels;

namespace DirectX12GameEngine.Editor.Messages
{
    public class LaunchStorageItemMessage
    {
        public LaunchStorageItemMessage(StorageItemViewModel item)
        {
            Item = item;
        }

        public StorageItemViewModel Item { get; }
    }
}
