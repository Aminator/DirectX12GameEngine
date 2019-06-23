using System;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Assets
{
    [AssetContentType(typeof(Texture))]
    public class TextureAsset : AssetWithSource<Texture>
    {
        private readonly ContentManager contentManager;
        private readonly GraphicsDevice device;

        public TextureAsset(ContentManager contentManager, GraphicsDevice device)
        {
            this.contentManager = contentManager;
            this.device = device;
        }

        public async override Task CreateAssetAsync(Texture texture)
        {
            string extension = Path.GetExtension(Source);

            if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
            {
                using Stream stream = await contentManager.RootFolder.OpenStreamForReadAsync(Source);
                using Image image = await Image.LoadAsync(stream);

                texture.AttachToGraphicsDevice(device);
                texture.InitializeFrom(image.Description);
                texture.Recreate(image.Data.Span);
            }
            else
            {
                throw new NotSupportedException("This file type is not supported.");
            }
        }
    }
}
