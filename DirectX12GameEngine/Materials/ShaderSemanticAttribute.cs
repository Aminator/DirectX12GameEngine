using System;

namespace DirectX12GameEngine
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public abstract class ShaderSemanticAttribute : Attribute
    {
    }

    public class PositionSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class NormalSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class TextureCoordinateSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class SystemInstanceIdSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class ColorSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class TangentSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class SystemPositionSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class SystemTargetSemanticAttribute : ShaderSemanticAttribute
    {
    }

    public class SystemRenderTargetArrayIndexSemanticAttribute : ShaderSemanticAttribute
    {
    }
}
