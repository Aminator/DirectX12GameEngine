using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IComputeColor : IComputeNode
    {
        Vector4 Compute();
    }
}
