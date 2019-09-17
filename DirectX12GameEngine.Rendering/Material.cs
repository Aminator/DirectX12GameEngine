using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Materials;

namespace DirectX12GameEngine.Rendering
{
    [TypeConverter(typeof(AssetReferenceTypeConverter))]
    public class Material
    {
        public MaterialDescriptor? Descriptor { get; set; }

        public IList<MaterialPass> Passes { get; } = new List<MaterialPass>();

        public static Task<Material> CreateAsync(GraphicsDevice device, MaterialDescriptor descriptor, IContentManager contentManager)
        {
            Material material = new Material { Descriptor = descriptor };

            MaterialGeneratorContext context = new MaterialGeneratorContext(device, material, contentManager);
            return MaterialGenerator.GenerateAsync(descriptor, context);
        }
    }
}
