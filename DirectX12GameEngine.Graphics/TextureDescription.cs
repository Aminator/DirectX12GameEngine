namespace DirectX12GameEngine.Graphics
{
    public struct TextureDescription
    {
        public TextureDimension Dimension;

        public int Width;

        public int Height;

        public short DepthOrArraySize;

        public short MipLevels;

        public PixelFormat Format;

        public int SampleCount;

        public GraphicsHeapType HeapType;

        public TextureFlags Flags;

        public static TextureDescription New2D(int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, short mipCount = 1, short arraySize = 1, int sampleCount = 1, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return new TextureDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                DepthOrArraySize = arraySize,
                SampleCount = sampleCount,
                Flags = textureFlags,
                Format = format,
                MipLevels = mipCount,
                HeapType = heapType,
            };
        }

        public static implicit operator TextureDescription(ImageDescription description)
        {
            return new TextureDescription()
            {
                Dimension = description.Dimension,
                Width = description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = description.Format,
                Flags = TextureFlags.ShaderResource,
                SampleCount = 1,
                HeapType = GraphicsHeapType.Default
            };
        }

        public static implicit operator ImageDescription(TextureDescription description)
        {
            return new ImageDescription()
            {
                Dimension = description.Dimension,
                Width = description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = description.Format,
            };
        }
    }
}
