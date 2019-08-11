namespace DirectX12GameEngine.Graphics
{
    public struct ImageDescription
    {
        public TextureDimension Dimension;

        public int Width;

        public int Height;

        public int DepthOrArraySize;

        public int MipLevels;

        public PixelFormat Format;

        public static ImageDescription New2D(int width, int height, PixelFormat format, int mipCount = 1, int arraySize = 1)
        {
            return new ImageDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                DepthOrArraySize = arraySize,
                Format = format,
                MipLevels = mipCount
            };
        }
    }
}
