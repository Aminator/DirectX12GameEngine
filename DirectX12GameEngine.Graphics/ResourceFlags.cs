using System;

namespace DirectX12GameEngine.Graphics
{
    [Flags]
    public enum ResourceFlags
    {
        None = 0,
        VertexBuffer = 1,
        IndexBuffer = 2,
        ConstantBuffer = 4,
        ShaderResource = 8,
        StreamOutput = 16,
        RenderTarget = 32,
        DepthStencil = 64,
        UnorderedAccess = 128,
        Decoder = 512,
        VideoEncoder = 1024
    }
}
