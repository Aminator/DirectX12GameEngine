namespace DirectX12GameEngine.Rendering.Materials.Brdf
{
    public interface IMaterialSpecularMicrofacetVisibilityFunction : IMaterialSpecularMicrofacetFunction
    {
        float Compute();
    }
}
