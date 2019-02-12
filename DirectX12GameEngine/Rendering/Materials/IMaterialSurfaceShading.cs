using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialSurfaceShading : IComputeNode
    {
        Vector3 ComputeDirectLightContribution();

        Vector3 ComputeEnvironmentLightContribution();
    }
}
