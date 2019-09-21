namespace DirectX12GameEngine.Graphics
{
    public struct GraphicsBufferDescription
    {
        public GraphicsBufferDescription(int sizeInBytes, GraphicsBufferFlags bufferFlags, GraphicsHeapType heapType, int structureByteStride = 0)
        {
            SizeInBytes = sizeInBytes;
            Flags = bufferFlags;
            HeapType = heapType;
            StructureByteStride = structureByteStride;
        }

        public int SizeInBytes;

        public GraphicsBufferFlags Flags;

        public GraphicsHeapType HeapType;

        public int StructureByteStride;
    }
}
