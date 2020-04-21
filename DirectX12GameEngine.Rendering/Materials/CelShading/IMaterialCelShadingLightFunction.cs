using System.Numerics;

namespace DirectX12GameEngine.Rendering.Materials.CelShading
{
    public interface IMaterialCelShadingLightFunction : IComputeShader
    {
        Vector3 Compute(float LightIn);
    }
}
