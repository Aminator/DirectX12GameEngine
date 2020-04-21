using System;
using System.Numerics;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12GameEngine.Shaders
{
    [Sampler]
    public readonly struct SamplerResource
    {
    }

    [Sampler]
    public readonly struct SamplerComparisonResource
    {
    }

    [ShaderResourceView]
    public readonly struct Texture2DResource
    {
        public Vector4 Sample(SamplerResource sampler, Vector2 texCoord) => throw new NotImplementedException();
    }

    [ShaderResourceView]
    public readonly struct Texture2DResource<T> where T : unmanaged
    {
        public T Sample(SamplerResource sampler, Vector2 texCoord) => throw new NotImplementedException();
    }

    [UnorderedAccessView]
    public readonly struct RWTexture2DResource<T> where T : unmanaged
    {
        public T this[Int2 index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    [ShaderResourceView]
    public readonly struct Texture2DArrayResource
    {
    }

    [ShaderResourceView]
    public readonly struct TextureCubeResource
    {
    }

    [ShaderResourceView]
    public readonly struct BufferResource<T>
    {
        public T this[int index] { get => throw new NotImplementedException(); }
    }

    [UnorderedAccessView]
    public readonly struct RWBufferResource<T>
    {
        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    [ShaderResourceView]
    public readonly struct StructuredBufferResource<T>
    {
        public T this[int index] { get => throw new NotImplementedException(); }
    }

    [UnorderedAccessView]
    public readonly struct RWStructuredBufferResource<T>
    {
        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
