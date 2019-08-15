namespace DirectX12GameEngine.Graphics
{
    public struct BufferDescription
    {
        public BufferDescription(int sizeInBytes, BufferFlags bufferFlags, GraphicsHeapType heapType, int structuredByteStride = 0)
        {
            SizeInBytes = sizeInBytes;
            Flags = bufferFlags;
            HeapType = heapType;
            StructuredByteStride = structuredByteStride;
        }

        public int SizeInBytes;

        public BufferFlags Flags;

        public GraphicsHeapType HeapType;

        public int StructuredByteStride;
    }
}
