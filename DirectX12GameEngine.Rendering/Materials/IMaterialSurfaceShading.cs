using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialSurfaceShading : IComputeShader
    {
        Vector3 ComputeDirectLightContribution();

        Vector3 ComputeEnvironmentLightContribution();
    }
}
