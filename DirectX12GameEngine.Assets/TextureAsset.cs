using System;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Assets
{
    [AssetContentType(typeof(Texture))]
    public class TextureAsset : AssetWithSource<Texture>
    {
        public bool IsSRgb { get; set; }

        public async override Task CreateAssetAsync(Texture texture, IServiceProvider services)
        {
            IContentManager contentManager = services.GetRequiredService<IContentManager>();
            GraphicsDevice device = services.GetRequiredService<GraphicsDevice>();

            if (device is null) throw new InvalidOperationException();

            using Stream stream = await contentManager.FileProvider.OpenStreamAsync(Source, FileMode.Open, FileAccess.Read);
            using Image image = await Image.LoadAsync(stream, IsSRgb);

            texture.Dispose();
            texture.GraphicsDevice = device;
            texture.InitializeFrom(image.Description);
            texture.SetData(image.Data.Span);
        }
    }
}
