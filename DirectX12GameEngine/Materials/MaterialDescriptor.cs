namespace DirectX12GameEngine
{
    public class MaterialDescriptor
    {
        public MaterialAttributes Attributes { get; set; } = new MaterialAttributes();

        public void Visit(Material material)
        {
            Attributes.Visit(material);
        }
    }
}
