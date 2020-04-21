using System;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Shaders
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
    public class ShaderContractAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IgnoreShaderMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ShaderMemberAttribute : Attribute
    {
        public ShaderMemberAttribute([CallerLineNumber] int order = 0) : this (null, order)
        {
        }

        public ShaderMemberAttribute(Type? resourceType, [CallerLineNumber] int order = 0)
        {
            Order = order;
            ResourceType = resourceType;
        }

        public int Order { get; }

        public Type? ResourceType { get; }

        public bool Override { get; set; }
    }

    public class ConstantBufferViewAttribute : ShaderMemberAttribute
    {
        public ConstantBufferViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public ConstantBufferViewAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class ShaderResourceViewAttribute : ShaderMemberAttribute
    {
        public ShaderResourceViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public ShaderResourceViewAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class UnorderedAccessViewAttribute : ShaderMemberAttribute
    {
        public UnorderedAccessViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public UnorderedAccessViewAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class SamplerAttribute : ShaderMemberAttribute
    {
        public SamplerAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public SamplerAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }

    public class StaticResourceAttribute : ShaderMemberAttribute
    {
        public StaticResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }

        public StaticResourceAttribute(Type? resourceType, [CallerLineNumber] int order = 0) : base(resourceType, order)
        {
        }
    }
}
