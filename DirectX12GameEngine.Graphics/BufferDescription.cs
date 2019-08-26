namespace DirectX12GameEngine.Graphics
{
    public struct BufferDescription
    {
        public BufferDescription(int sizeInBytes, BufferFlags bufferFlags, GraphicsHeapType heapType, int structureByteStride = 0)
        {
            SizeInBytes = sizeInBytes;
            Flags = bufferFlags;
            HeapType = heapType;
            StructureByteStride = structureByteStride;
        }

        public int SizeInBytes;

        public BufferFlags Flags;

        public GraphicsHeapType HeapType;

        public int StructureByteStride;
    }
}
