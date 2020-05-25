using System.Numerics;

namespace DirectX12GameEngine.Rendering
{
    public interface IColorShader : IShader
    {
        Vector4 ComputeColor(in SamplingContext context);
    }
}
