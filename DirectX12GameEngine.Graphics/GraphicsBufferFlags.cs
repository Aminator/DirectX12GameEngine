using System;

namespace DirectX12GameEngine.Graphics
{
    [Flags]
    public enum GraphicsBufferFlags
    {
        None = 0,
        RenderTarget = 1,
        ConstantBuffer = 2,
        IndexBuffer = 4,
        VertexBuffer = 8,
        ShaderResource = 16,
        UnorderedAccess = 32
    }
}
