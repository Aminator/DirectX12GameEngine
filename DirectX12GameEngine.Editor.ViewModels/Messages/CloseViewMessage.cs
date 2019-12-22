namespace DirectX12GameEngine.Editor.Messages
{
    public class CloseViewMessage
    {
        public CloseViewMessage(string viewName)
        {
            ViewName = viewName;
        }

        public string ViewName { get; }
    }
}
