using System.Collections.Generic;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Materials;

namespace DirectX12GameEngine.Rendering
{
    public class Material
    {
        public MaterialDescriptor? Descriptor { get; set; }

        public IList<MaterialPass> Passes { get; } = new List<MaterialPass>();

        public static Material Create(GraphicsDevice device, MaterialDescriptor descriptor)
        {
            Material material = new Material { Descriptor = descriptor };

            MaterialGeneratorContext context = new MaterialGeneratorContext(device, material);
            return MaterialGenerator.Generate(descriptor, context);
        }
    }
}
