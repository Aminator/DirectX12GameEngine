using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Materials;

namespace DirectX12GameEngine.Rendering
{
    public class Material
    {
        public MaterialDescriptor? Descriptor { get; set; }

        public IList<MaterialPass> Passes { get; } = new List<MaterialPass>();

        public static Task<Material> CreateAsync(GraphicsDevice device, MaterialDescriptor descriptor, ShaderContentManager contentManager)
        {
            Material material = new Material { Descriptor = descriptor };

            MaterialGeneratorContext context = new MaterialGeneratorContext(device, material, contentManager);
            return MaterialGenerator.GenerateAsync(descriptor, context);
        }
    }
}
