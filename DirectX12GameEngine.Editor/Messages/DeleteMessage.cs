namespace DirectX12GameEngine.Editor.Messages
{
    public class DeleteMessage
    {
        public DeleteMessage(object objectToDelete)
        {
            ObjectToDelete = objectToDelete;
        }

        public object ObjectToDelete { get; }
    }
}
