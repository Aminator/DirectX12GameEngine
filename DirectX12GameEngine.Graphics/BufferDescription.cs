namespace DirectX12GameEngine.Graphics
{
    public struct BufferDescription
    {
        public BufferDescription(int sizeInBytes, BufferFlags bufferFlags, GraphicsResourceUsage usage)
        {
            SizeInBytes = sizeInBytes;
            Flags = bufferFlags;
            Usage = usage;
        }

        public int SizeInBytes;

        public BufferFlags Flags;

        public GraphicsResourceUsage Usage;
    }
}
