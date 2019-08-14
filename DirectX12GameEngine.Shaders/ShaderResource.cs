using System;
using System.Numerics;

namespace DirectX12GameEngine.Shaders
{
    public abstract class ShaderResource
    {
    }

    [Sampler]
    public class SamplerResource : ShaderResource
    {
    }

    [Sampler]
    public class SamplerComparisonResource : ShaderResource
    {
    }

    [Texture]
    public class Texture2DResource : ShaderResource
    {
        public Vector4 Sample(SamplerResource sampler, Vector2 texCoord) => throw new NotImplementedException();
    }

    [Texture]
    public class Texture2DResource<T> : ShaderResource where T : unmanaged
    {
        public T Sample(SamplerResource sampler, Vector2 texCoord) => throw new NotImplementedException();
    }

    [Texture]
    public class Texture2DArrayResource : ShaderResource
    {
    }

    [Texture]
    public class TextureCubeResource : ShaderResource
    {
    }

    [UnorderedAccessView]
    public class RWBufferResource<T> : ShaderResource
    {
        public T this[uint index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    [UnorderedAccessView]
    public class RWTexture2DResource<T> : ShaderResource where T : unmanaged
    {
        public T this[Numerics.UInt2 index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
