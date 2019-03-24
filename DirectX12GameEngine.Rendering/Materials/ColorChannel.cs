using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public enum ColorChannel
    {
        [ShaderResource] R,
        [ShaderResource] G,
        [ShaderResource] B,
        [ShaderResource] A
    }
}
