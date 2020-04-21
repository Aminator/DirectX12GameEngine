using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IComputeColor : IComputeShader
    {
        Vector4 Compute();
    }
}
