using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials.CelShading
{
    public interface IMaterialCelShadingLightFunction : IComputeNode
    {
        Vector3 Compute(float LightIn);
    }
}
