namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    public interface IMaterialSpecularMicrofacetNormalDistributionFunction : IMaterialSpecularMicrofacetFunction
    {
        float Compute();
    }
}
