namespace DirectX12GameEngine.Rendering
{
    public interface IComputeShader
    {
        void Accept(ShaderGeneratorContext context);
    }
}
