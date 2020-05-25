namespace DirectX12GameEngine.Rendering
{
    public interface IShader
    {
        void Accept(ShaderGeneratorContext context);
    }
}
