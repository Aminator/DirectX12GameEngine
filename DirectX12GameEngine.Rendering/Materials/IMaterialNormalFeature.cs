using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialNormalFeature : IShader
    {
        Vector3 ComputeNormal(in SamplingContext context);
    }
}
