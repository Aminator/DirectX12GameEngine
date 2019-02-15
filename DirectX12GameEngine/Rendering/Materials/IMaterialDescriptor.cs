namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialDescriptor : IComputeNode
    {
        MaterialAttributes Attributes { get; set; }
    }
}
