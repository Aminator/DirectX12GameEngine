using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Assets
{
    [AssetContentType(typeof(Model))]
    public class ModelAsset : AssetWithSource<Model>
    {
        public IList<Material> Materials { get; } = new List<Material>();

        public async override Task CreateAssetAsync(Model model, IServiceProvider services)
        {
            ContentManager contentManager = services.GetRequiredService<ContentManager>();
            GraphicsDevice device = services.GetRequiredService<GraphicsDevice>();

            if (device is null) throw new InvalidOperationException();

            string extension = Path.GetExtension(Source);

            if (extension == ".glb")
            {
                model.Materials.Clear();
                model.Meshes.Clear();

                using (Stream stream = await contentManager.RootFolder.OpenStreamForReadAsync(Source))
                {
                    var meshes = await new GltfModelLoader(device).LoadMeshesAsync(stream);

                    foreach (Mesh mesh in meshes)
                    {
                        model.Meshes.Add(mesh);
                    }
                }

                foreach (Material material in Materials)
                {
                    model.Materials.Add(material);
                }
            }
            else
            {
                throw new NotSupportedException("This file type is not supported.");
            }
        }
    }
}
