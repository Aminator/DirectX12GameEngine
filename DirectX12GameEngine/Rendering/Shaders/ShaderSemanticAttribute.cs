using System;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Rendering.Shaders
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public abstract class ShaderSemanticAttribute : ShaderResourceAttribute
    {
        public ShaderSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class PositionSemanticAttribute : ShaderSemanticAttribute
    {
        public PositionSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class NormalSemanticAttribute : ShaderSemanticAttribute
    {
        public NormalSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class TextureCoordinateSemanticAttribute : ShaderSemanticAttribute
    {
        public TextureCoordinateSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class SystemInstanceIdSemanticAttribute : ShaderSemanticAttribute
    {
        public SystemInstanceIdSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class ColorSemanticAttribute : ShaderSemanticAttribute
    {
        public ColorSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class TangentSemanticAttribute : ShaderSemanticAttribute
    {
        public TangentSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class SystemPositionSemanticAttribute : ShaderSemanticAttribute
    {
        public SystemPositionSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class SystemTargetSemanticAttribute : ShaderSemanticAttribute
    {
        public SystemTargetSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class SystemRenderTargetArrayIndexSemanticAttribute : ShaderSemanticAttribute
    {
        public SystemRenderTargetArrayIndexSemanticAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }
}
