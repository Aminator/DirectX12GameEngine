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

        public TextureFlags Flags;

        public SampleDescription SampleDescription;

        public GraphicsHeapType HeapType;

        public static TextureDescription New2D(int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0, GraphicsHeapType heapType = GraphicsHeapType.Default)
        {
            return new TextureDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                DepthOrArraySize = arraySize,
                MipLevels = mipLevels,
                Format = format,
                Flags = textureFlags,
                SampleDescription = new SampleDescription(sampleCount, sampleQuality),
                HeapType = heapType
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
                SampleDescription = new SampleDescription(1, 0),
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
