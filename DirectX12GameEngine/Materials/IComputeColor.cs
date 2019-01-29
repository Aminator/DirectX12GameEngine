using System.Numerics;

namespace DirectX12GameEngine
{
    public interface IComputeColor : IComputeNode
    {
        Vector4 Compute(Vector2 texCoord);
    }
}
