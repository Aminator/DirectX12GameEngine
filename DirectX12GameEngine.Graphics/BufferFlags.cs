using System;

namespace DirectX12GameEngine.Graphics
{
    [Flags]
    public enum BufferFlags
    {
        None = 0,
        ConstantBuffer = 1,
        IndexBuffer = 2,
        VertexBuffer = 4,
        ShaderResource = 8,
        UnorderedAccess = 16,
    }
}
