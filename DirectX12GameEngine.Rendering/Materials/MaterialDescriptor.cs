namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialDescriptor : IMaterialDescriptor
    {
        public MaterialAttributes Attributes { get; set; } = new MaterialAttributes();

        public void Visit(MaterialGeneratorContext context)
        {
            Attributes.Visit(context);
        }
    }
}
