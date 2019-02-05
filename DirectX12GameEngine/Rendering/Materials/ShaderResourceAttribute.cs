using System;

namespace DirectX12GameEngine.Rendering.Materials
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public abstract class ShaderResourceAttribute : Attribute
    {
    }

    public class ConstantBufferResourceAttribute : ShaderResourceAttribute
    {
    }

    public class SamplerResourceAttribute : ShaderResourceAttribute
    {
    }

    public class SamplerComparisonResourceAttribute : ShaderResourceAttribute
    {
    }

    public class Texture2DResourceAttribute : ShaderResourceAttribute
    {
    }

    public class Texture2DArrayResourceAttribute : ShaderResourceAttribute
    {
    }

    public class TextureCubeResourceAttribute : ShaderResourceAttribute
    {
    }
}
