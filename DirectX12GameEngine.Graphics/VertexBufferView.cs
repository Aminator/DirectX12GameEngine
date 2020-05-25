namespace DirectX12GameEngine.Graphics
{
    public struct VertexBufferView
    {
        public long BufferLocation;

        public int SizeInBytes;

        public int StrideInBytes;

        public VertexBufferView(GraphicsResource resource, int strideInBytes)
            : this(resource, (int)resource.Width, strideInBytes)
        {
        }

        public VertexBufferView(GraphicsResource resource, int sizeInBytes, int strideInBytes)
            : this(resource.NativeResource.GPUVirtualAddress, sizeInBytes, strideInBytes)
        {
        }

        public VertexBufferView(long bufferLocation, int sizeInBytes, int strideInBytes)
        {
            BufferLocation = bufferLocation;
            SizeInBytes = sizeInBytes;
            StrideInBytes = strideInBytes;
        }
    }
}
