namespace DirectX12GameEngine.Editor.Messages
{
    public class ShowPropertiesMessage
    {
        public ShowPropertiesMessage(object obj)
        {
            Object = obj;
        }

        public object Object { get; }
    }
}
