namespace DirectX12GameEngine.Input
{
    public class Cursor
    {
        public Cursor(CursorType type, uint id)
        {
            Type = type;
            Id = id;
        }

        public uint Id { get; }

        public CursorType Type { get; }
    }
}
