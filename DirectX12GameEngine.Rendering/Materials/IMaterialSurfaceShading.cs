using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialSurfaceShading : IShader
    {
        Vector3 ComputeDirectLightContribution(in MaterialShadingContext context);

        Vector3 ComputeEnvironmentLightContribution(in MaterialShadingContext context);
    }
}
