namespace DirectX12GameEngine.Graphics
{
    public class RootDescriptorTable
    {
        public RootDescriptorTable(params DescriptorRange[] ranges)
        {
            Ranges = ranges;
        }

        public DescriptorRange[] Ranges { get; set; }
    }
}
