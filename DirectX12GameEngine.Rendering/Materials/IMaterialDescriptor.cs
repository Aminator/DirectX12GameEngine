using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialDescriptor : IComputeNode, IIdentifiable
    {
        MaterialAttributes Attributes { get; set; }
    }
}
