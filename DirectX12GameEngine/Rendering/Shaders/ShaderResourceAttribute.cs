using System;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Rendering.Shaders
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ShaderResourceAttribute : Attribute
    {
        public ShaderResourceAttribute([CallerLineNumber] int order = 0)
        {
            Order = order;
        }

        public int Order { get; }

        public bool Override { get; set; }
    }

    public class ConstantBufferResourceAttribute : ShaderResourceAttribute
    {
        public ConstantBufferResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class SamplerResourceAttribute : ShaderResourceAttribute
    {
        public SamplerResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class SamplerComparisonResourceAttribute : ShaderResourceAttribute
    {
        public SamplerComparisonResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class Texture2DResourceAttribute : ShaderResourceAttribute
    {
        public Texture2DResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class Texture2DArrayResourceAttribute : ShaderResourceAttribute
    {
        public Texture2DArrayResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class TextureCubeResourceAttribute : ShaderResourceAttribute
    {
        public TextureCubeResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class StaticResourceAttribute : ShaderResourceAttribute
    {
        public StaticResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }
}
