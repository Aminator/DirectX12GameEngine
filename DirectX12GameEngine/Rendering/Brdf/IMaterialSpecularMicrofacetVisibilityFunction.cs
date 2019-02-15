namespace DirectX12GameEngine.Rendering.Brdf
{
    public interface IMaterialSpecularMicrofacetVisibilityFunction : IMaterialSpecularMicrofacetFunction
    {
        float Compute();
    }
}
