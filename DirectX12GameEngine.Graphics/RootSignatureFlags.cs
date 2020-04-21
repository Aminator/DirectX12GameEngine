using System;

namespace DirectX12GameEngine.Graphics
{
    [Flags]
    public enum RootSignatureFlags
    {
        None = 0,
        AllowInputAssemblerInputLayout = 1,
        DenyVertexShaderRootAccess = 2,
        DenyHullShaderRootAccess = 4,
        DenyDomainShaderRootAccess = 8,
        DenyGeometryShaderRootAccess = 16,
        DenyPixelShaderRootAccess = 32,
        AllowStreamOutput = 64,
        LocalRootSignature = 128
    }
}
