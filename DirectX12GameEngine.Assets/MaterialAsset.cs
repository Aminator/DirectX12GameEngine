using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Assets
{
    [AssetContentType(typeof(Material))]
    public class MaterialAsset : AssetWithSource<Material>
    {
        public MaterialAttributes Attributes { get; set; } = new MaterialAttributes();

        public async override Task CreateAssetAsync(Material material, IServiceProvider services)
        {
            ContentManager contentManager = services.GetRequiredService<ContentManager>();
            ShaderContentManager shaderContentManager = services.GetRequiredService<ShaderContentManager>();
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
                    using Stream stream = await contentManager.RootFolder.OpenStreamForReadAsync(path);
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

            material.Passes.Clear();

            material.Descriptor = descriptor;

            MaterialGeneratorContext context = new MaterialGeneratorContext(device, material, shaderContentManager);
            await MaterialGenerator.GenerateAsync(descriptor, context);
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
