namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialRoughnessFeature : IShader
    {
        float ComputeRoughness(in SamplingContext context);
    }
}
