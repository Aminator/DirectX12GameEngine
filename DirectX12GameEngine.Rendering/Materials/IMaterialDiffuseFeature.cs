using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialDiffuseFeature : IShader
    {
        Vector4 ComputeDiffuseColor(in SamplingContext context);
    }
}
