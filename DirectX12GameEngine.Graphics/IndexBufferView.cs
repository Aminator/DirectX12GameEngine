namespace DirectX12GameEngine.Graphics
{
    public struct IndexBufferView
    {
        public long BufferLocation;

        public int SizeInBytes;

        public PixelFormat Format;

        public IndexBufferView(GraphicsResource resource, bool is32Bit = false)
            : this(resource, (int)resource.Width, is32Bit)
        {
        }

        public IndexBufferView(GraphicsResource resource, int sizeInBytes, bool is32Bit = false)
            : this(resource.NativeResource.GPUVirtualAddress, sizeInBytes, is32Bit)
        {
        }

        public IndexBufferView(long bufferLocation, int sizeInBytes, bool is32Bit = false)
            : this(bufferLocation, sizeInBytes, is32Bit ? PixelFormat.R32UInt : PixelFormat.R16UInt)
        {
        }

        public IndexBufferView(GraphicsResource resource, PixelFormat format)
            : this(resource, (int)resource.Width, format)
        {
        }

        public IndexBufferView(GraphicsResource resource, int sizeInBytes, PixelFormat format)
            : this(resource.NativeResource.GPUVirtualAddress, sizeInBytes, format)
        {
        }

        public IndexBufferView(long bufferLocation, int sizeInBytes, PixelFormat format)
        {
            BufferLocation = bufferLocation;
            SizeInBytes = sizeInBytes;
            Format = format;
        }
    }
}
