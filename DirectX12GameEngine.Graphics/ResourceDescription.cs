namespace DirectX12GameEngine.Graphics
{
    public struct ResourceDescription
    {
        public ResourceDimension Dimension;

        public long Alignment;

        public long Width;

        public int Height;

        public short DepthOrArraySize;

        public short MipLevels;

        public PixelFormat Format;

        public SampleDescription SampleDescription;

        public TextureLayout Layout;

        public ResourceFlags Flags;

        public static ResourceDescription Buffer(int sizeInBytes, ResourceFlags bufferFlags)
        {
            return new ResourceDescription
            {
                Dimension = ResourceDimension.Buffer,
                Width = sizeInBytes,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Layout = TextureLayout.RowMajor,
                Flags = bufferFlags,
            };
        }

        public static ResourceDescription Texture2D(int width, int height, PixelFormat format, ResourceFlags textureFlags = ResourceFlags.None, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0)
        {
            return new ResourceDescription
            {
                Dimension = ResourceDimension.Texture2D,
                Width = width,
                Height = height,
                DepthOrArraySize = arraySize,
                MipLevels = mipLevels,
                Format = format,
                SampleDescription = new SampleDescription(sampleCount, sampleQuality),
                Flags = textureFlags
            };
        }

        public static implicit operator ResourceDescription(ImageDescription description)
        {
            return new ResourceDescription()
            {
                Dimension = description.Dimension,
                Width = description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = description.Format,
                SampleDescription = new SampleDescription(1, 0),
                Flags = ResourceFlags.None
            };
        }

        public static implicit operator ImageDescription(ResourceDescription description)
        {
            return new ImageDescription()
            {
                Dimension = description.Dimension,
                Width = (int)description.Width,
                Height = description.Height,
                DepthOrArraySize = description.DepthOrArraySize,
                MipLevels = description.MipLevels,
                Format = description.Format,
            };
        }
    }
}
