using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Assets
{
    public class ModelAsset : AssetWithSource
    {
        public IList<Material> Materials { get; } = new List<Material>();

        public async override Task<object> CreateAssetAsync(IServiceProvider services)
        {
            IContentManager contentManager = services.GetRequiredService<IContentManager>();
            GraphicsDevice device = services.GetRequiredService<GraphicsDevice>();

            if (device is null) throw new InvalidOperationException();

            string extension = Path.GetExtension(Source);

            if (extension == ".glb")
            {
                Model model = new Model();

                using Stream stream = await contentManager.FileProvider.OpenStreamAsync(Source, FileMode.Open, FileAccess.Read);
                GltfModelLoader modelLoader = await GltfModelLoader.CreateAsync(device, stream);

                var meshes = await modelLoader.GetMeshesAsync();

                foreach (Mesh mesh in meshes)
                {
                    model.Meshes.Add(mesh);
                }

                if (Materials.Count > 0)
                {
                    foreach (Material material in Materials)
                    {
                        model.Materials.Add(material);
                    }
                }
                else
                {
                    foreach (MaterialAttributes attributes in await modelLoader.GetMaterialAttributesAsync())
                    {
                        Material material = await Material.CreateAsync(device, new MaterialDescriptor { Id = Id, Attributes = attributes }, contentManager);
                        model.Materials.Add(material);
                    }
                }

                return model;
            }
            else
            {
                throw new NotSupportedException("This file type is not supported.");
            }
        }
    }
}
