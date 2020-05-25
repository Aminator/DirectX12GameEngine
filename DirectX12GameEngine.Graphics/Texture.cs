using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    [TypeConverter(typeof(AssetReferenceTypeConverter))]
    public sealed class Texture : GraphicsResource
    {
        public Texture(GraphicsDevice device, ResourceDescription description, HeapType heapType) : base(device, description, heapType)
        {
            if (description.Dimension < ResourceDimension.Texture1D) throw new ArgumentException();
        }

        internal Texture(GraphicsDevice device, ID3D12Resource resource) : base(device, resource)
        {
        }

        public static async Task<Texture> LoadAsync(GraphicsDevice device, string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            return await LoadAsync(device, stream);
        }

        public static async Task<Texture> LoadAsync(GraphicsDevice device, Stream stream)
        {
            Image image = await Image.LoadAsync(stream);
            return Create2D(device, image.Data.Span, image.Width, image.Height, image.Description.Format);
        }

        public static Texture Create2D(GraphicsDevice device, int width, int height, PixelFormat format, ResourceFlags textureFlags = ResourceFlags.None, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0, HeapType heapType = HeapType.Default)
        {
            return new Texture(device, ResourceDescription.Texture2D(width, height, format, textureFlags, mipLevels, arraySize, sampleCount, sampleQuality), heapType);
        }

        public static Texture Create2D<T>(GraphicsDevice device, Span<T> data, int width, int height, PixelFormat format, ResourceFlags textureFlags = ResourceFlags.None, short mipLevels = 1, short arraySize = 1, int sampleCount = 1, int sampleQuality = 0, HeapType heapType = HeapType.Default) where T : unmanaged
        {
            Texture texture = Create2D(device, width, height, format, textureFlags, mipLevels, arraySize, sampleCount, sampleQuality, heapType);
            texture.SetData(data);

            return texture;
        }
    }
}
