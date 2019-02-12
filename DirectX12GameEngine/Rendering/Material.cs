using System.Collections.Generic;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Materials;


namespace DirectX12GameEngine.Rendering
{
    public class Material
    {
#nullable disable
        public Material()
        {
        }
#nullable enable

        public Material(GraphicsDevice device, MaterialDescriptor descriptor)
        {
            Descriptor = descriptor;

            MaterialGeneratorContext context = new MaterialGeneratorContext(device, this);
            MaterialGenerator.Generate(descriptor, context);
        }

        public MaterialDescriptor Descriptor { get; set; }

        public IList<MaterialPass> Passes { get; } = new List<MaterialPass>();
    }
}
