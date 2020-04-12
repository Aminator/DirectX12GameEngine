using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Assets
{
    public class MaterialAsset : AssetWithSource
    {
        public MaterialAttributes Attributes { get; set; } = new MaterialAttributes();

        public async override Task<object> CreateAssetAsync(IServiceProvider services)
        {
            IContentManager contentManager = services.GetRequiredService<IContentManager>();
            GraphicsDevice device = services.GetRequiredService<GraphicsDevice>();

            if (device is null) throw new InvalidOperationException();

            MaterialDescriptor descriptor;

            if (string.IsNullOrEmpty(Source))
            {
                descriptor = new MaterialDescriptor { Id = Id, Attributes = Attributes };
            }
            else
            {
                string path = Source;
                int index = GetIndex(ref path);
                string extension = Path.GetExtension(path);

                if (extension == ".glb")
                {
                    using Stream stream = await contentManager.FileProvider.OpenStreamAsync(path, FileMode.Open, FileAccess.Read);
                    GltfModelLoader modelLoader = await GltfModelLoader.CreateAsync(device, stream);
                    MaterialAttributes materialAttributes = await modelLoader.GetMaterialAttributesAsync(index);

                    // TODO: Combine material attributes.

                    descriptor = new MaterialDescriptor { Id = Id, Attributes = materialAttributes };
                }
                else
                {
                    throw new NotSupportedException("This file type is not supported.");
                }
            }

            return await Material.CreateAsync(device, descriptor, contentManager);
        }

        private static int GetIndex(ref string path)
        {
            Match match = Regex.Match(path, @"\[\d+\]$");
            int index = 0;

            if (match.Success)
            {
                path = path.Remove(match.Index);
                index = int.Parse(match.Value.Trim('[', ']'));
            }

            return index;
        }
    }
}
