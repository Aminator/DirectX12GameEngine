namespace DirectX12GameEngine.Graphics
{
    public struct TextureDescription
    {
        public TextureDimension Dimension;

        public int Width;

        public int Height;

        public int Depth;

        public int ArraySize;

        public int MipLevels;

        public PixelFormat Format;

        public int MultisampleCount;

        public GraphicsResourceUsage Usage;

        public TextureFlags Flags;

        public static TextureDescription New2D(int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int mipCount = 1, int arraySize = 1, int multisampleCount = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return new TextureDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                Depth = 1,
                ArraySize = arraySize,
                MultisampleCount = multisampleCount,
                Flags = textureFlags,
                Format = format,
                MipLevels = mipCount,
                Usage = usage,
            };
        }
    }
}
