using System;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Assets
{
    [AssetContentType(typeof(Texture))]
    public class TextureAsset : AssetWithSource<Texture>
    {
        public async override Task CreateAssetAsync(Texture texture, IServiceProvider services)
        {
            ContentManager contentManager = services.GetRequiredService<ContentManager>();
            GraphicsDevice device = services.GetRequiredService<GraphicsDevice>();

            if (device is null) throw new InvalidOperationException();

            string extension = Path.GetExtension(Source);

            if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
            {
                using Stream stream = await contentManager.RootFolder.OpenStreamForReadAsync(Source);
                using Image image = await Image.LoadAsync(stream);

                texture.Dispose();
                texture.GraphicsDevice = device;
                texture.InitializeFrom(image.Description);
                texture.SetData(image.Data.Span);
            }
            else
            {
                throw new NotSupportedException("This file type is not supported.");
            }
        }
    }
}
