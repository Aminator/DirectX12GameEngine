using System;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Shaders
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
    public class ShaderContractAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IgnoreShaderMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ShaderMemberAttribute : Attribute
    {
        public ShaderMemberAttribute([CallerLineNumber] int order = 0)
        {
            Order = order;
        }

        public int Order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public abstract class ShaderResourceAttribute : ShaderMemberAttribute
    {
        public ShaderResourceAttribute([CallerLineNumber] int order = 0) : this(null, order)
        {
        }

        public ShaderResourceAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(order)
        {
            ResourceType = resourceType;
        }

        public Type? ResourceType { get; set; }

        public bool Override { get; set; }
    }

    public class ConstantBufferViewAttribute : ShaderResourceAttribute
    {
        public ConstantBufferViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public ConstantBufferViewAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class ShaderResourceViewAttribute : ShaderResourceAttribute
    {
        public ShaderResourceViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public ShaderResourceViewAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class UnorderedAccessViewAttribute : ShaderResourceAttribute
    {
        public UnorderedAccessViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public UnorderedAccessViewAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class SamplerAttribute : ShaderResourceAttribute
    {
        public SamplerAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public SamplerAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class StaticResourceAttribute : ShaderResourceAttribute
    {
        public StaticResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public StaticResourceAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }
}
