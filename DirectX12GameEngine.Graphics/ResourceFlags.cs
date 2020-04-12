using System;

namespace DirectX12GameEngine.Graphics
{
    [Flags]
    public enum ResourceFlags
    {
        None = 0,
        AllowRenderTarget = 1,
        AllowDepthStencil = 2,
        AllowUnorderedAccess = 4,
        DenyShaderResource = 8,
        AllowCrossAdapter = 16,
        AllowSimultaneousAccess = 32,
        VideoDecodeReferenceOnly = 64
    }
}
