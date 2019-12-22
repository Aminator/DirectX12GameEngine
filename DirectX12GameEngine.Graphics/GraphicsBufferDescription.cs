namespace DirectX12GameEngine.Graphics
{
    public struct GraphicsBufferDescription
    {
        public GraphicsBufferDescription(int sizeInBytes, ResourceFlags bufferFlags, GraphicsHeapType heapType, int structureByteStride = 0)
        {
            SizeInBytes = sizeInBytes;
            Flags = bufferFlags;
            HeapType = heapType;
            StructureByteStride = structureByteStride;
        }

        public int SizeInBytes;

        public ResourceFlags Flags;

        public GraphicsHeapType HeapType;

        public int StructureByteStride;
    }
}
