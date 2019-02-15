namespace DirectX12GameEngine.Rendering.Brdf
{
    public interface IMaterialSpecularMicrofacetNormalDistributionFunction : IMaterialSpecularMicrofacetFunction
    {
        float Compute();
    }
}
