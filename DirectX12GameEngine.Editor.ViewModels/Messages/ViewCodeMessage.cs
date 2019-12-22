using DirectX12GameEngine.Editor.ViewModels;

namespace DirectX12GameEngine.Editor.Messages
{
    public class ViewCodeMessage
    {
        public ViewCodeMessage(StorageFileViewModel file)
        {
            File = file;
        }

        public StorageFileViewModel File { get; }
    }
}
