using ShaderGen;
using System;

namespace DirectX12GameEngine
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ResourceAttribute : Attribute
    {
        public ResourceAttribute(ShaderResourceKind kind)
        {
            Kind = kind;
        }

        public ShaderResourceKind Kind { get; }
    }

    public class SamplerResourceAttribute : ResourceAttribute
    {
        public SamplerResourceAttribute() : base(ShaderResourceKind.Sampler)
        {
        }
    }

    public class Texture2DResourceAttribute : ResourceAttribute
    {
        public Texture2DResourceAttribute() : base(ShaderResourceKind.Texture2D)
        {
        }
    }

    public class UniformResourceAttribute : ResourceAttribute
    {
        public UniformResourceAttribute() : base(ShaderResourceKind.Uniform)
        {
        }
    }
}
