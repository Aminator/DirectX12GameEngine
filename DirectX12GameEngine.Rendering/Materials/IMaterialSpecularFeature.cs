using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialSpecularFeature : IShader
    {
        Vector3 ComputeSpecularColor(in SamplingContext context, ref Vector3 diffuseColor);
    }
}
