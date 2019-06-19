using System;
using System.IO;
using System.Threading.Tasks;
using SharpDX.DXGI;
using SharpDX.WIC;

namespace DirectX12GameEngine.Graphics
{
    public sealed class Image : IDisposable
    {
        public Memory<byte> Data { get; }

        public ImageDescription Description { get; }

        public int Width => Description.Width;

        public int Height => Description.Height;

        internal Image()
        {
        }

        internal Image(ImageDescription description, Memory<byte> data)
        {
            Description = description;
            Data = data;
        }

        public static async Task<Image> LoadAsync(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            return await LoadAsync(stream);
        }

        public static Task<Image> LoadAsync(Stream stream)
        {
            return Task.Run(() =>
            {
                ImagingFactory2 imagingFactory = new ImagingFactory2();
                BitmapDecoder decoder = new BitmapDecoder(imagingFactory, stream, DecodeOptions.CacheOnDemand);

                FormatConverter bitmapSource = new FormatConverter(imagingFactory);
                bitmapSource.Initialize(decoder.GetFrame(0), SharpDX.WIC.PixelFormat.Format32bppBGRA);

                PixelFormat pixelFormat = PixelFormat.B8G8R8A8_UNorm;
                int stride = bitmapSource.Size.Width * FormatHelper.SizeOfInBytes((Format)pixelFormat);
                byte[] imageBuffer = new byte[stride * bitmapSource.Size.Height];

                bitmapSource.CopyPixels(imageBuffer, stride);

                ImageDescription description = ImageDescription.New2D(bitmapSource.Size.Width, bitmapSource.Size.Height, pixelFormat);

                return new Image(description, imageBuffer);
            });
        }

        public void Dispose()
        {
        }
    }
}
