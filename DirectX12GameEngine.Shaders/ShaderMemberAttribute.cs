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
        public ShaderMemberAttribute([CallerLineNumber] int order = 0)
        {
            Order = order;
        }

        public int Order { get; }

        public bool Override { get; set; }
    }

    public class ConstantBufferViewAttribute : ShaderMemberAttribute
    {
        public ConstantBufferViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class ShaderResourceViewAttribute : ShaderMemberAttribute
    {
        public ShaderResourceViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class UnorderedAccessViewAttribute : ShaderMemberAttribute
    {
        public UnorderedAccessViewAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class SamplerAttribute : ShaderMemberAttribute
    {
        public SamplerAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class StaticResourceAttribute : ShaderMemberAttribute
    {
        public StaticResourceAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }
}
