namespace DirectX12GameEngine.Editor.Messages
{
    public class OpenViewMessage
    {
        public OpenViewMessage(string viewName)
        {
            ViewName = viewName;
        }

        public string ViewName { get; }
    }
}
