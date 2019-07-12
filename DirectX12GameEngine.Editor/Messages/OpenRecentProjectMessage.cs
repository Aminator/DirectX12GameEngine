namespace DirectX12GameEngine.Editor.Messages
{
    public class OpenRecentProjectMessage
    {
        public OpenRecentProjectMessage(string token)
        {
            Token = token;
        }

        public string Token { get; }
    }
}
