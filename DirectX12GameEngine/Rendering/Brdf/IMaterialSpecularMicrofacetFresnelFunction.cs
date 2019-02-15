using System.Numerics;

namespace DirectX12GameEngine.Rendering.Brdf
{
    public interface IMaterialSpecularMicrofacetFresnelFunction : IMaterialSpecularMicrofacetFunction
    {
        Vector3 Compute(Vector3 f0);
    }
}
