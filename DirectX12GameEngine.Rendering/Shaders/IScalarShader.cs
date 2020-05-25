namespace DirectX12GameEngine.Rendering
{
    public interface IScalarShader : IShader
    {
        float ComputeScalar(in SamplingContext context);
    }
}
